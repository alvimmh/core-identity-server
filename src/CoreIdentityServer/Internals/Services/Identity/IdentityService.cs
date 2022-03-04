using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Models.InputModels;
using Microsoft.AspNetCore.Routing;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Constants.UserActions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Encodings.Web;
using System.Security.Claims;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Storage;
using System.Collections.Generic;

namespace CoreIdentityServer.Internals.Services.Identity.IdentityService
{
    public class IdentityService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private EmailService EmailService;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        private readonly UrlEncoder UrlEncoder;
        private bool ResourcesDisposed;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            UrlEncoder urlEncoder
        ) {
            UserManager = userManager;
            SignInManager = signInManager;
            EmailService = emailService;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            UrlEncoder = urlEncoder;
        }

        public async Task<object[]> ManageEmailChallenge(string defaultRoute, string returnUrl = null)
        {
            EmailChallengeInputModel model = null;
            string redirectRoute = defaultRoute;

            // check if user is signed in
            bool userEmailExists = false;
            bool currentUserSignedIn = CheckActiveSession();
            string userEmail = null;

            if (currentUserSignedIn)
            {
                ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

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
                // retain TempData so page reload keeps user on the same page
                TempData.Keep();

                string resendEmailRecordId = resendEmailRecordIdExists ? resendEmailRecordIdTempData.ToString() : null;

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    model = await GenerateEmailChallengeInputModel(userEmail, resendEmailRecordId, returnUrl);
                }
            }

            return GenerateArray(model, redirectRoute);
        }

        private async Task<EmailChallengeInputModel> GenerateEmailChallengeInputModel(string userEmail, string resendEmailRecordId, string returnUrl)
        {
            // return null to redirect to another page
            EmailChallengeInputModel model = null;

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                ApplicationUser user = await UserManager.FindByEmailAsync(userEmail);

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
                    // user exists with registered account and confirmed email, so user is either signing in or trying to reset TOTP access

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

            if (!string.IsNullOrWhiteSpace(resendEmailRecordId))
                await EmailService.ArchiveEmailRecord(resendEmailRecordId, user);

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
                case UserActionContexts.ResetTOTPAccessEmailChallenge:
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
            string redirectRoute = null;

            bool userMeetsSignInPrerequisites = await VerifySignInPrerequisites(user);

            if (!userMeetsSignInPrerequisites)
            {
                // add generic error and return ViewModel
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");

                return redirectRoute;
            }

            await ResetSignInAttempts(user);

            string sessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

            string resendEmailRecordId = await EmailService.SendNewSessionVerificationEmail(
                AutomatedEmails.NoReply,
                user.Email,
                user.UserName,
                sessionVerificationCode
            );

            TempData[TempDataKeys.UserEmail] = user.Email;
            TempData[TempDataKeys.ResendEmailRecordId] = resendEmailRecordId.ToString();

            redirectRoute = GenerateRouteUrl("EmailChallenge", "Authentication", "Access");

            return redirectRoute;
        }

        public async Task<string> SignIn(ApplicationUser user)
        {
            string redirectRoute = null;

            bool userMeetsSignInPrerequisites = await VerifySignInPrerequisites(user);

            if (!userMeetsSignInPrerequisites)
            {
                // add generic error and return ViewModel
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");

                return redirectRoute;
            }

            await ResetSignInAttempts(user);

            // update security stamp of the user so other active sessions are logged out on the next request
            IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(user);

            if (updateSecurityStamp.Succeeded)
            {
                await SignInManager.SignInAsync(user, false);

                // send email to user about new session
                // temporarily disabling this email
                // await EmailService.SendNewActiveSessionNotificationEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                // clear all unnecessary temp data
                TempData.Clear();

                redirectRoute = GenerateRouteUrl("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
            }
            else
            {
                Console.WriteLine($"Error updating security stamp");

                foreach (IdentityError error in updateSecurityStamp.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
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
                Console.WriteLine($"Error updating user");

                foreach (IdentityError error in updateUser.Errors)
                    Console.WriteLine(error.Description);
                
                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                // reset TOTP access request acknowledgement failed, return ViewModel
                return redirectRoute;
            }

            // sign out user
            await SignOut();

            TempData[TempDataKeys.UserEmail] = user.Email;

            // redirect user to Register TOTP Access page
            return GenerateRouteUrl("RegisterTOTPAccess", "SignUp", "Enroll");
        }

        private async Task<string> ConfirmTOTPChallenge(ApplicationUser user, string targetRoute)
        {
            string redirectRoute = null;
            DateTime authorizationExpiryDateTime = DateTime.UtcNow.AddMinutes(5);
            bool userHasExpiredClaim = ActionContext.HttpContext.User.HasClaim(
                claim => claim.Type == ProjectClaimTypes.TOTPAuthorizationExpiry
            );
            Claim newClaim = new Claim(ProjectClaimTypes.TOTPAuthorizationExpiry, authorizationExpiryDateTime.ToString());

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

            IdentityResult addNewUserClaim = addNewUserClaim = await UserManager.AddClaimAsync(user, newClaim);

            if (!addNewUserClaim.Succeeded)
            {
                Console.WriteLine($"Error adding user claim of type ${ProjectClaimTypes.TOTPAuthorizationExpiry}");

                // log errors
                foreach (IdentityError error in addNewUserClaim.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                return redirectRoute;
            }

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
                    Console.WriteLine($"Error updating security stamp during user signout");

                    foreach (IdentityError error in updateSecurityStamp.Errors)
                        Console.WriteLine(error.Description);
                }
            }

            // delete all TempData
            TempData.Clear();

            // but delete authentication cookie anyways
            await SignInManager.SignOutAsync();
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

        private async Task<bool> VerifySignInPrerequisites(ApplicationUser user)
        {
            // check if user doesn't exist with the given email
            if (user == null)
                return false;

            // if user exists but did not complete registration, send email to complete registration
            if (!user.AccountRegistered)
            {
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                return false;
            }

            // if user exists but requires authenticator reset, send email to reset
            if (user.AccountRegistered && user.RequiresAuthenticatorReset)
            {
                await EmailService.SendResetTOTPAccessReminderEmail(AutomatedEmails.NoReply, user.Email, user.Email);

                return false;
            }

            // check if user is allowed to sign in && user is not locked out            
            bool canUserSignIn = await SignInManager.CanSignInAsync(user);
            bool appSupportsLockout = UserManager.SupportsUserLockout;
            bool isUserLockedOut = await UserManager.IsLockedOutAsync(user);

            if (!canUserSignIn || (appSupportsLockout && isUserLockedOut))
                return false;

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
