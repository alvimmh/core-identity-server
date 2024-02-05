using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Constants.UserActions;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Storage;
using CoreIdentityServer.Internals.Constants.Account;
using IdentityModel;
using System.Linq;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using CoreIdentityServer.Internals.Constants.Events;
using CoreIdentityServer.Internals.Models.ViewModels;
using Mapster;
using System.Net.Http;
using CoreIdentityServer.Internals.Constants.Authentication;
using CoreIdentityServer.Internals.Constants.Errors;

namespace CoreIdentityServer.Internals.Services.Identity.IdentityService
{
    public class IdentityService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private readonly IBackChannelLogoutService DefaultBackChannelLogoutService;
        private EmailService EmailService;
        private OIDCTokenService OIDCTokenService;
        private ActionContext ActionContext;
        private readonly HttpClient HttpClient;
        private readonly ITempDataDictionary TempData;
        private readonly UrlEncoder UrlEncoder;
        private bool ResourcesDisposed;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IBackChannelLogoutService defaultBackChannelLogoutService,
            EmailService emailService,
            OIDCTokenService oidcTokenService,
            IActionContextAccessor actionContextAccessor,
            HttpClient httpClient,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            UrlEncoder urlEncoder
        ) {
            UserManager = userManager;
            SignInManager = signInManager;
            DefaultBackChannelLogoutService = defaultBackChannelLogoutService;
            EmailService = emailService;
            OIDCTokenService = oidcTokenService;
            ActionContext = actionContextAccessor.ActionContext;
            HttpClient = httpClient;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            UrlEncoder = urlEncoder;
        }

        public async Task<object[]> ManageTOTPAccessRecoveryChallenge(string defaultRoute)
        {
            TOTPAccessRecoveryChallengeInputModel model = null;
            string redirectRoute = defaultRoute;

            // check if user is signed in
            bool userEmailExists = false;
            bool currentUserSignedIn = CheckActiveSession();
            string userEmail = null;
            ApplicationUser user = null;

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user != null)
                {
                    userEmailExists = true;
                    userEmail = user.Email;
                }
            }
            else
            {
                userEmailExists = TempData.TryGetValue(TempDataKeys.UserEmail, out object userEmailTempData);

                if (userEmailExists)
                    userEmail = userEmailTempData.ToString();
            }

            if (userEmailExists)
            {
                // retain TempData so page reload keeps user on the same page
                TempData.Keep();

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    model = await GenerateTOTPAccessRecoveryChallengeInputModel(userEmail, user);
                }
            }

            return GenerateArray(model, redirectRoute);
        }

        private async Task<TOTPAccessRecoveryChallengeInputModel> GenerateTOTPAccessRecoveryChallengeInputModel(string userEmail, ApplicationUser challengedUser = null)
        {
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                ApplicationUser user = challengedUser ?? await UserManager.FindByEmailAsync(userEmail);

                if (user == null)
                {
                    // user doesn't exist, return null to redirect to another page
                    return null;
                }
                else if (user.EmailConfirmed && !user.AccountRegistered)
                {
                    // user exists with confirmed email and unregistered account, send email to complete registration
                    await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, userEmail, user.UserName);

                    return null;
                }
                else if (user.EmailConfirmed && user.AccountRegistered)
                {
                    // user exists with registered account and confirmed email, so user is trying to reset TOTP access
                    return new TOTPAccessRecoveryChallengeInputModel
                    {
                        Email = userEmail
                    };
                }
            }

            return null;
        } 

        public async Task<object[]> ManageEmailChallenge(string defaultRoute, string returnUrl = null)
        {
            EmailChallengeInputModel model = null;
            string redirectRoute = defaultRoute;

            // check if user is signed in
            bool userEmailExists = false;
            ApplicationUser user = null;
            bool currentUserSignedIn = CheckActiveSession();
            string userEmail = null;

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user != null)
                {
                    userEmailExists = true;
                    userEmail = user.Email;
                }
            }
            else
            {
                userEmailExists = TempData.TryGetValue(TempDataKeys.UserEmail, out object userEmailTempData);

                if (userEmailExists)
                    userEmail = userEmailTempData.ToString();
            }

            bool resendEmailRecordIdExists = TempData.TryGetValue(
                TempDataKeys.ResendEmailRecordId,
                out object resendEmailRecordIdTempData
            );

            if (userEmailExists)
            {
                // retain TempData as user isn't signed in, so page reload keeps user on the same page
                if (!currentUserSignedIn)
                    TempData.Keep();

                string resendEmailRecordId = resendEmailRecordIdExists ? resendEmailRecordIdTempData.ToString() : null;

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    model = await GenerateEmailChallengeInputModel(userEmail, resendEmailRecordId, returnUrl, user);
                }
            }

            return GenerateArray(model, redirectRoute);
        }

        private async Task<EmailChallengeInputModel> GenerateEmailChallengeInputModel(string userEmail, string resendEmailRecordId, string returnUrl, ApplicationUser challengedUser = null)
        {
            // return null to redirect to another page
            EmailChallengeInputModel model = null;

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                ApplicationUser user = challengedUser ?? await UserManager.FindByEmailAsync(userEmail);

                if (user == null)
                {
                    // user doesn't exist
                    return model;
                }
                else if (user.EmailConfirmed && !user.AccountRegistered)
                {
                    // user exists with confirmed email and unregistered account, send email to complete registration
                    await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, userEmail, user.UserName);

                    return model;
                }
                else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
                {
                    // user exists with unregistered account and unconfirmed email, so user is signing up
                    // or
                    // user exists with registered account and confirmed email, so user is signing in

                    model = new EmailChallengeInputModel
                    {
                        Email = userEmail,
                        ResendEmailRecordId = resendEmailRecordId,
                        ReturnUrl = returnUrl
                    };

                    return model;
                }
            }

            return model;
        }

        public async Task<string> ManageTOTPChallengeSuccess(
            ApplicationUser user,
            string resendEmailRecordId,
            string context,
            string targetRoute
        ) {
            string redirectRoute = null;

            // delete an email record as it has served its purpose
            if (!string.IsNullOrWhiteSpace(resendEmailRecordId))
                await EmailService.DeleteEmailRecord(resendEmailRecordId, user);

            switch (context)
            {
                case UserActionContexts.ConfirmEmailChallenge:
                    redirectRoute = await ConfirmUserEmail(user);
                    break;
                case UserActionContexts.SignInTOTPChallenge:
                    redirectRoute = await SignInTOTP(user);
                    break;
                case UserActionContexts.SignInEmailChallenge:
                    redirectRoute = await SignIn(user);
                    break;
                case UserActionContexts.ResetTOTPAccessRecoveryChallenge:
                    redirectRoute = await AcknowledgeResetTOTPAccessRequest(user);
                    break;
                case UserActionContexts.TOTPChallenge:
                    redirectRoute = await ConfirmTOTPChallenge(user, targetRoute);
                    break;
                default:
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                    break;
            }

            return redirectRoute;
        }

        private async Task<string> ConfirmUserEmail(ApplicationUser user)
        {
            string redirectRoute = null;

            user.EmailConfirmed = true;

            IdentityResult updateUser = await UserManager.UpdateAsync(user);

            if (updateUser.Succeeded)
            {
                // temporarily disabling this email
                // await EmailService.SendEmailConfirmedEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                TempData[TempDataKeys.UserEmail] = user.Email;
                SetTempDataExpiryDateTime(TempData);

                redirectRoute = GenerateRouteUrl("RegisterTOTPAccess", "SignUp", "Enroll");
            }
            else
            {
                Console.WriteLine($"Error updating user");

                // add errors to ModelState
                foreach (IdentityError error in updateUser.Errors)
                    ActionContext.ModelState.AddModelError(string.Empty, error.Description);
            }

            return redirectRoute;
        }

        private async Task<string> SignInTOTP(ApplicationUser user)
        {
            bool twoFactorAuthenticationEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

            if (!twoFactorAuthenticationEnabled)
            {
                string redirectRoute = await SignIn(user);

                return redirectRoute;
            }

            await ResetSignInAttempts(user);

            string emailVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

            string resendEmailRecordId = await EmailService.SendNewSessionVerificationEmail(
                AutomatedEmails.NoReply,
                user.Email,
                user.UserName,
                emailVerificationCode
            );

            TempData[TempDataKeys.UserEmail] = user.Email;
            TempData[TempDataKeys.ResendEmailRecordId] = resendEmailRecordId.ToString();

            return GenerateRouteUrl("EmailChallenge", "Authentication", "Access");
        }

        public async Task<string> SignIn(ApplicationUser user)
        {
            string redirectRoute = null;

            await ResetSignInAttempts(user);

            // update security stamp of the user so other active sessions are logged out on the next request
            IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(user);

            if (updateSecurityStamp.Succeeded)
            {
                // setting amr claim with value "mfa"
                // Duende Identity Server will set it to "pwd" by default if we don't set it
                Claim AMRClaim = CreateMFATypeAMRClaim();
                IList<Claim> additionalClaims = new List<Claim> { AMRClaim };

                Console.WriteLine("Setting 'idp' claim overriding value: mfa");
                await SignInManager.SignInWithClaimsAsync(user, false, additionalClaims);

                user.UpdateLastSignedInTimeStamp();

                // record sign in timestamps
                IdentityResult updateUser = await UserManager.UpdateAsync(user);

                if (!updateUser.Succeeded)
                {
                    Console.WriteLine("Error updating user");

                    foreach (IdentityError error in updateUser.Errors)
                        Console.WriteLine(error.Description);
                    
                    ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");

                    // could not update user information, so signout user and redirect to sign in page
                    await SignOut();

                    return redirectRoute;
                }
                else
                {
                    // send email to user about new session
                    // temporarily disabling this email
                    // await EmailService.SendNewActiveSessionNotificationEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                    // clear all unnecessary temp data
                    TempData.Clear();

                    redirectRoute = GenerateRouteUrl("Dashboard", "Pages", "General");
                }
            }
            else
            {
                Console.WriteLine($"Error updating security stamp");

                foreach (IdentityError error in updateSecurityStamp.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
            }

            return redirectRoute;
        }

        private async Task<string> AcknowledgeResetTOTPAccessRequest(ApplicationUser user)
        {
            string redirectRoute = null;

            user.RequiresAuthenticatorReset = true;

            IdentityResult updateUser = await UserManager.UpdateAsync(user);

            if (!updateUser.Succeeded)
            {
                Console.WriteLine("Error updating user");

                foreach (IdentityError error in updateUser.Errors)
                    Console.WriteLine(error.Description);
                
                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again with a different recovery code.");

                // reset TOTP access request acknowledgement failed, return null to return ViewModel
                return redirectRoute;
            }

            // sign out user
            await SignOut();

            TempData[TempDataKeys.UserEmail] = user.Email;
            SetTempDataExpiryDateTime(TempData);

            // redirect user to Register TOTP Access page
            return GenerateRouteUrl("RegisterTOTPAccess", "SignUp", "Enroll");
        }

        private async Task<string> ConfirmTOTPChallenge(ApplicationUser user, string targetRoute)
        {
            string redirectRoute = null;
            bool userHasExpiredClaim = ActionContext.HttpContext.User.HasClaim(
                claim => claim.Type == ProjectClaimTypes.TOTPAuthorizationExpiry
            );
            Claim newClaim = CreateTOTPAutorizationExpiryClaim();

            if (userHasExpiredClaim)
            {
                IEnumerable<Claim> expiredClaims = ActionContext.HttpContext.User.FindAll(
                    claim => claim.Type == ProjectClaimTypes.TOTPAuthorizationExpiry
                );

                IdentityResult deleteExpiredClaims = await UserManager.RemoveClaimsAsync(user, expiredClaims);

                if (!deleteExpiredClaims.Succeeded)
                {
                    Console.WriteLine("Error deleting expired user claims");

                    // log errors
                    foreach (IdentityError error in deleteExpiredClaims.Errors)
                        Console.WriteLine(error.Description);

                    ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                    return redirectRoute;
                }
            }

            IdentityResult addNewUserClaim = await UserManager.AddClaimAsync(user, newClaim);

            if (!addNewUserClaim.Succeeded)
            {
                Console.WriteLine($"Error adding user claim of type {ProjectClaimTypes.TOTPAuthorizationExpiry}");

                // log errors
                foreach (IdentityError error in addNewUserClaim.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                return redirectRoute;
            }

            // refreshing user sign in so the claim updates take effect immediately
            await SignInManager.RefreshSignInAsync(user);

            return targetRoute;
        }

        public async Task SignOut()
        {
            ApplicationUser currentUser = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (currentUser != null)
            {
                // the authentication cookie can be reset manually. So if its compromised,
                // then someone can bypass authentication when using the application
                // so the session needs to be explicitly invalidated by updating the security stamp
                IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(currentUser);

                if (!updateSecurityStamp.Succeeded)
                {
                    Console.WriteLine("Error updating security stamp during user signout");

                    foreach (IdentityError error in updateSecurityStamp.Errors)
                        Console.WriteLine(error.Description);
                }
            }

            // delete all TempData
            TempData.Clear();

            // but delete authentication cookie anyways
            await SignInManager.SignOutAsync();
        }

        // send notifications to all clients of CIS to logout the user
        //
        // this will trigger back-channel logout notifications to all clients
        // if the user is persisted in the client, the client will sign out the user
        // otherwise the client will return a failed HTTP response
        //
        // note, different clients may implement different types of user authentication system,
        // this method only sends the back-channel notification for these clients
        //
        // it is the client's responsibility to signout the user from their system
        public async Task SendBackChannelLogoutNotificationsForUserAsync(ApplicationUser user)
        {
            List<string> allClients = Config.Clients.Select(client => client.ClientId).ToList();

            // note, SessionId is required, so not setting it to null
            LogoutNotificationContext blockedUserLogoutNotificationContext = new LogoutNotificationContext()
            {
                SubjectId = user.Id,
                ClientIds = allClients,
                SessionId = string.Empty
            };

            await DefaultBackChannelLogoutService.SendLogoutNotificationsAsync(blockedUserLogoutNotificationContext);
        }

        public async Task<IdentityResult> SendBackChannelDeleteNotificationsForUserAsync(ApplicationUser user)
        {
            List<ClientNotificationViewModel> allClients = Config.Clients
                                                                    .Select(client => client.Adapt<ClientNotificationViewModel>())
                                                                    .ToList();

            List<BackChannelNotificationRequest> notificationRequests = new List<BackChannelNotificationRequest>();

            foreach (ClientNotificationViewModel client in allClients)
            {
                CreateTokenInputModel inputModel = new CreateTokenInputModel()
                {
                    SubjectId = user.Id,
                    ClientId = client.ClientId
                };

                string notificationJWTToken = await OIDCTokenService.CreateTokenAsync(inputModel, OIDCTokenEvents.BackChannelDelete);

                string notificationEndpoint = $"{client.ClientUri}/administration/authentication/delete_oidc";

                BackChannelNotificationRequest notificationRequest = new BackChannelNotificationRequest()
                {
                    NotificationType = BackChannelNotificationTypes.Delete,
                    JWTToken = notificationJWTToken,
                    ClientId = client.ClientId,
                    ClientNotificationUri = notificationEndpoint
                };

                notificationRequests.Add(notificationRequest);
            }

            Task<bool>[] sendNotificationTasks = notificationRequests.Select(SendBackChannelNotificationAsync).ToArray();
        
            bool[] sendNotificationTaskResults = await Task.WhenAll<bool>(sendNotificationTasks);

            bool tasksSucceeded = !sendNotificationTaskResults.Any(element => element == false);

            if (!tasksSucceeded)
            {
                bool tasksSucceededPartially = sendNotificationTaskResults.Any(element => element == true);

                // user was deleted from some clients but not all
                if (tasksSucceededPartially)
                {
                    IdentityError error = new IdentityError()
                    {
                        Description = InternalCustomErrors.UserPartiallyDeleted
                    };

                    return IdentityResult.Failed(error);
                }

                return IdentityResult.Failed();
            }

            return IdentityResult.Success;
        }

        private async Task<bool> SendBackChannelNotificationAsync(BackChannelNotificationRequest request)
        {
            Dictionary<string, string> data = CreateFormPostPayloadAsync(request);

            return await PostBackChannelNotificationJwt(request, data);
        }

        private Dictionary<string, string> CreateFormPostPayloadAsync(BackChannelNotificationRequest request)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { CustomTokenOptions.BackChannelDeleteTokenPostBodyKey, request.JWTToken }
            };

            return data;
        }

        private Task<bool> PostBackChannelNotificationJwt(
            BackChannelNotificationRequest request,
            Dictionary<string, string> data
        ) {
            return PostBackChannelNotificationAsync(request.ClientNotificationUri, data, request.NotificationType);
        }

        private async Task<bool> PostBackChannelNotificationAsync(string url, Dictionary<string, string> payload, string notificationType)
        {
            try
            {
                HttpResponseMessage response = await HttpClient.PostAsync(url, new FormUrlEncodedContent(payload));
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Response from back-channel {0} notification endpoint: {1} status code: {2}", notificationType, url, (int)response.StatusCode);

                    return true;
                }
                else
                {
                    Console.WriteLine("Response from {0} notification endpoint: {1} status code: {2}", notificationType, url, (int)response.StatusCode);
                
                    return false;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception invoking back-channel {0} for url: {1}", notificationType, url);
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.InnerException);

                return false;
            }
        }

        public async Task RefreshUserSignIn(ApplicationUser user)
        {
            // delete all TempData
            TempData.Clear();

            await SignInManager.RefreshSignInAsync(user);
        }

        public async Task<List<string>> GenerateTOTPRecoveryCodes(ApplicationUser user, int numberOfRecoveryCodes)
        {
            List<string> recoveryCodes = new List<string>();

            while (recoveryCodes.Count < numberOfRecoveryCodes)
            {
                recoveryCodes = (await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, numberOfRecoveryCodes)).ToList();
            }

            return recoveryCodes;
        }

        public async Task RecordUnsuccessfulSignInAttempt(ApplicationUser user)
        {
            IdentityResult saveUnsuccessfulAttempt = await UserManager.AccessFailedAsync(user);
            if (!saveUnsuccessfulAttempt.Succeeded)
            {
                Console.WriteLine($"Could not record unsuccessful SignIn attempt.");

                foreach (IdentityError error in saveUnsuccessfulAttempt.Errors)
                    Console.WriteLine(error.Description);
            }

            // if increasing the failed count results in account lockout, notify the user
            bool isUserLockedOut = await UserManager.IsLockedOutAsync(user);
            if (isUserLockedOut)
                await EmailService.SendAccountLockedOutEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
        }

        private Claim CreateMFATypeAMRClaim()
        {
            return new Claim(JwtClaimTypes.AuthenticationMethod, ProjectClaimValues.AMRTypeMFA);
        }

        private Claim CreateTOTPAutorizationExpiryClaim()
        {
            DateTime authorizationExpiryDateTime = DateTime.UtcNow.AddSeconds(AccountOptions.TOTPAuthorizationDurationInSeconds);

            return new Claim(ProjectClaimTypes.TOTPAuthorizationExpiry, authorizationExpiryDateTime.ToString());
        }

        private async Task ResetSignInAttempts(ApplicationUser user)
        {
            IdentityResult resetAttempts = await UserManager.ResetAccessFailedCountAsync(user);

            if (!resetAttempts.Succeeded)
            {
                Console.WriteLine($"Cound not record successful SignIn attempt.");

                foreach (IdentityError error in resetAttempts.Errors)
                    Console.WriteLine(error.Description);
            }
        }

        public async Task<bool> VerifySignInPrerequisites(ApplicationUser user)
        {
            // check if user doesn't exist with the given email
            if (user == null)
                return false;

            if (user.Archived)
                return false;

            if (user.Blocked)
            {
                Console.WriteLine($"User with id {user.Id} is blocked.");
                return false;
            }
            
            if (!user.EmailConfirmed)
            {
                Console.WriteLine($"User with id {user.Id} has unconfirmed email.");
                return false;
            }

            // if user exists but did not complete registration, send email to complete registration
            if (!user.AccountRegistered)
            {
                Console.WriteLine($"User with id {user.Id} has unregistered account.");

                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                return false;
            }

            // if user exists but requires authenticator reset, send email to reset
            if (user.AccountRegistered && user.RequiresAuthenticatorReset)
            {
                Console.WriteLine($"User with id {user.Id} needs to reset TOTP access authenticator.");

                await EmailService.SendResetTOTPAccessReminderEmail(AutomatedEmails.NoReply, user.Email, user.Email);

                return false;
            }

            // check if user is allowed to sign in && user is not locked out            
            bool canUserSignIn = await SignInManager.CanSignInAsync(user);
            bool appSupportsLockout = UserManager.SupportsUserLockout;
            bool isUserLockedOut = await UserManager.IsLockedOutAsync(user);

            if (!canUserSignIn || (appSupportsLockout && isUserLockedOut))
            {
                Console.WriteLine($"User with id {user.Id} cannot signin or is locked out.");

                return false;
            }

            return true;
        }

        // check if there is a current user logged in, if so redirect to an authorized page
        public bool CheckActiveSession()
        {
            return SignInManager.IsSignedIn(ActionContext.HttpContext.User);
        }

        public async Task<bool> VerifyTOTPCode(ApplicationUser user, string tokenProvider, string verificationCode)
        {
            // verify email challenge
            bool verificationResult = await UserManager.VerifyTwoFactorTokenAsync(
                user,
                tokenProvider,
                verificationCode
            );

            return verificationResult;
        }

        public async Task<bool> VerifyTOTPAccessRecoveryCode(ApplicationUser user, string verificationCode)
        {
            // verify TOTP access recovery code
            IdentityResult redeemTOTPAccessRecoveryCode = await UserManager.RedeemTwoFactorRecoveryCodeAsync(user, verificationCode);
        
            if (!redeemTOTPAccessRecoveryCode.Succeeded)
            {
                Console.WriteLine("Error redeeming user TOTP access recovery code");

                foreach (IdentityError error in redeemTOTPAccessRecoveryCode.Errors)
                    Console.WriteLine(error.Description);

                return false;
            }

            return true;
        }

        public string GenerateQRCodeUri(string email, string authenticatorKey)
        {
            const string AuthenticatorKeyUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

            return string.Format(
                AuthenticatorKeyUriFormat,
                UrlEncoder.Encode("Bonic"),
                UrlEncoder.Encode(email),
                authenticatorKey
            );
        }

        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}
