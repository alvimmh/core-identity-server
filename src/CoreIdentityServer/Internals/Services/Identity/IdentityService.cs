using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Services.Email.EmailService;
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

namespace CoreIdentityServer.Internals.Services.Identity.IdentityService
{
    public class IdentityService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private EmailService EmailService;
        private ActionContext ActionContext;
        private readonly UrlEncoder UrlEncoder;
        private bool ResourcesDisposed;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor,
            UrlEncoder urlEncoder
        ) {
            UserManager = userManager;
            SignInManager = signInManager;
            EmailService = emailService;
            ActionContext = actionContextAccessor.ActionContext;
            UrlEncoder = urlEncoder;
        }

        public async Task<object[]> ManageEmailChallenge(ITempDataDictionary tempData, RouteValueDictionary defaultRoute)
        {
            EmailChallengeInputModel model = null;
            RouteValueDictionary redirectRouteValues = defaultRoute;

            bool tempDataExists = tempData.TryGetValue("userEmail", out object tempDataValue);
            if (tempDataExists)
            {
                string userEmailFromTempData = tempDataValue.ToString();

                if (!string.IsNullOrWhiteSpace(userEmailFromTempData))
                {
                    model = await GenerateEmailChallengeInputModel(userEmailFromTempData);
                }
            }

            return GenerateArray(model, redirectRouteValues);
        }

        private async Task<EmailChallengeInputModel> GenerateEmailChallengeInputModel(string userEmail)
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
                    SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, userEmail, user.UserName);

                    return model;
                }
                else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
                {
                    // user exists with unregistered account and unconfirmed email, so user is signing up
                    // or
                    // user exists with registered account and confirmed email, so user is either signing in or trying to reset TOTP access

                    model = new EmailChallengeInputModel
                    {
                        Email = userEmail
                    };

                    return model;
                }
            }

            return model;
        }

        public async Task<RouteValueDictionary> VerifyEmailChallenge(EmailChallengeInputModel inputModel, RouteValueDictionary defaultRoute, RouteValueDictionary targetRoute, string tokenProvider, string context)
        {
            RouteValueDictionary redirectRouteValues = null;

            if (!ActionContext.ModelState.IsValid)
                return redirectRouteValues;

            redirectRouteValues = await VerifyTOTPChallenge(inputModel.Email, inputModel.VerificationCode, defaultRoute, targetRoute, tokenProvider, context);

            return redirectRouteValues;
        }

        public async Task<RouteValueDictionary> VerifyTOTPChallenge(string userEmail, string verificationCode, RouteValueDictionary defaultRoute, RouteValueDictionary targetRoute, string tokenProvider, string context)
        {
            RouteValueDictionary redirectRouteValues = null;

            ApplicationUser user = await UserManager.FindByEmailAsync(userEmail);

            if (user == null)
            {
                if (context == UserActionContexts.SignInTOTPChallenge || context == UserActionContexts.SignInEmailChallenge)
                {
                    // user doesn't exist, but don't reveal to end user
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
                }
                else
                {
                    // user doesn't exist, redirect to default route
                    redirectRouteValues = defaultRoute;
                }

                return redirectRouteValues;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, userEmail, user.UserName);
                redirectRouteValues = defaultRoute;

                return redirectRouteValues;
            }
            else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
            {
                // user exists with unregistered account and unconfirmed email, so user is signing up
                // or
                // user exists with registered account and confirmed email, so user is either signing in or trying to reset TOTP access

                // if TOTP code verified, redirect to target page
                bool TOTPCodeVerified = await VerifyTOTPCode(user, tokenProvider, verificationCode);

                if (TOTPCodeVerified)
                {
                    switch (context)
                    {
                        case UserActionContexts.ConfirmEmailChallenge:
                            redirectRouteValues = await ConfirmUserEmail(user);
                            break;
                        case UserActionContexts.SignInTOTPChallenge:
                            redirectRouteValues = await SignInTOTP(user);
                            break;
                        case UserActionContexts.SignInEmailChallenge:
                            redirectRouteValues = await SignIn(user);
                            break;
                        case UserActionContexts.ResetTOTPAccessEmailChallenge:
                            redirectRouteValues = await AcknowledgeResetTOTPAccessRequest(user);
                            break;
                        case UserActionContexts.TOTPChallenge:
                            redirectRouteValues = await ConfirmTOTPChallenge(user, targetRoute);
                            break;
                        default:
                            ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                            break;
                    }
                }
                else
                {
                    if (context == UserActionContexts.SignInTOTPChallenge || context == UserActionContexts.SignInEmailChallenge)
                    {
                        await RecordUnsuccessfulSignInAttempt(user);
                    }

                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                }
            }

            return redirectRouteValues;
        }

        private async Task<RouteValueDictionary> ConfirmUserEmail(ApplicationUser user)
        {
            RouteValueDictionary redirectRouteValues = null;

            user.EmailConfirmed = true;

            IdentityResult updateUser = await UserManager.UpdateAsync(user);

            if (updateUser.Succeeded)
            {
                SendEmailConfirmedEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccess", "SignUp", "Enroll");
            }
            else
            {
                Console.WriteLine($"Error updating user");

                // add errors to ModelState
                foreach (IdentityError error in updateUser.Errors)
                    ActionContext.ModelState.AddModelError(string.Empty, error.Description);
            }

            return redirectRouteValues;
        }

        private async Task<RouteValueDictionary> SignInTOTP(ApplicationUser user)
        {
            RouteValueDictionary redirectRouteValues = null;

            bool userMeetsSignInPrerequisites = await VerifySignInPrerequisites(user);

            if (!userMeetsSignInPrerequisites)
            {
                // add generic error and return ViewModel
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");

                return redirectRouteValues;
            }

            await ResetSignInAttempts(user);

            string sessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

            SendNewSessionVerificationEmail(AutomatedEmails.NoReply, user.Email, user.UserName, sessionVerificationCode);

            redirectRouteValues = GenerateRedirectRouteValues("EmailChallenge", "Authentication", "Access");

            return redirectRouteValues;
        }

        public async Task<RouteValueDictionary> SignIn(ApplicationUser user)
        {
            RouteValueDictionary redirectRouteValues = null;

            bool userMeetsSignInPrerequisites = await VerifySignInPrerequisites(user);

            if (!userMeetsSignInPrerequisites)
            {
                // add generic error and return ViewModel
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");

                return redirectRouteValues;
            }

            await ResetSignInAttempts(user);

            // update security stamp of the user so other active sessions are logged out on the next request
            IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(user);

            if (updateSecurityStamp.Succeeded)
            {
                await SignInManager.SignInAsync(user, false);

                // send email to user about new session
                SendNewActiveSessionNotificationEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
            }
            else
            {
                Console.WriteLine($"Error updating security stamp");

                foreach (IdentityError error in updateSecurityStamp.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
            }

            return redirectRouteValues;
        }

        private async Task<RouteValueDictionary> AcknowledgeResetTOTPAccessRequest(ApplicationUser user)
        {
            RouteValueDictionary redirectRouteValues = null;

            user.RequiresAuthenticatorReset = true;

            IdentityResult updateUser = await UserManager.UpdateAsync(user);

            if (!updateUser.Succeeded)
            {
                Console.WriteLine($"Error updating user");

                foreach (IdentityError error in updateUser.Errors)
                    Console.WriteLine(error.Description);
                
                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                // reset TOTP access request acknowledgement failed, return ViewModel
                return redirectRouteValues;
            }

            // sign out user
            await SignOut();

            // redirect user to Register TOTP Access page
            return GenerateRedirectRouteValues("RegisterTOTPAccess", "SignUp", "Enroll");
        }

        private async Task<RouteValueDictionary> ConfirmTOTPChallenge(ApplicationUser user, RouteValueDictionary targetRoute)
        {
            RouteValueDictionary redirectRouteValues = null;
            DateTime authorizationExpiryDateTime = DateTime.UtcNow.AddMinutes(5);
            Claim expiredClaim = ActionContext.HttpContext.User.FindFirst(
                claim => claim.Type == Claims.TOTPAuthorizationExpiry
            );
            Claim newClaim = new Claim(Claims.TOTPAuthorizationExpiry, authorizationExpiryDateTime.ToString());

            IdentityResult updateUserClaims = null;
            
            if (expiredClaim == null)
            {
                updateUserClaims = await UserManager.AddClaimAsync(user, newClaim);
            }
            else
            {
                updateUserClaims = await UserManager.ReplaceClaimAsync(user, expiredClaim, newClaim);
            }

            if (!updateUserClaims.Succeeded)
            {
                Console.WriteLine($"Error updating user claims");

                // add errors to ModelState
                foreach (IdentityError error in updateUserClaims.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                return redirectRouteValues;
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

            // but delete authentication cookie anyways
            await SignInManager.SignOutAsync();
        }

        private async Task RecordUnsuccessfulSignInAttempt(ApplicationUser user)
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
                SendAccountLockedOutEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
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
                SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                return false;
            }

            // if user exists but requires authenticator reset, send email to reset
            if (user.AccountRegistered && user.RequiresAuthenticatorReset)
            {
                SendResetTOTPAccessReminderEmail(AutomatedEmails.NoReply, user.Email, user.Email);

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

        private async Task<bool> VerifyTOTPCode(ApplicationUser user, string tokenProvider, string verificationCode)
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

        // send a reminder to user to reset authenticator
        public void SendResetTOTPAccessReminderEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "Please Reset TOTP Access";
            string emailBody = $"Dear {userName}, you previously tried to reset your TOTP authenticator but did not reset it completely. To keep your account secure, we have blocked your latest Sign In attempt. Please finish resetting your authenticator to Sign In.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // send a verification code to verify user's identity before resetting TOTP access
        public void SendResetTOTPAccessVerificationEmail(string emailFrom, string emailTo, string userName, string verificationCode)
        {
            string emailSubject = "Please Confirm Your Identity";
            string emailBody = $"Greetings {userName}, please confirm you identity by submitting this verification code: {verificationCode}";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // send a verification code to verify user's email address
        public void SendEmailConfirmationEmail(string emailFrom, string emailTo, string userName, string verificationCode)
        {
            string emailSubject = "Please Confirm Your Email";
            string emailBody = $"Greetings {userName}, please confirm your email by submitting this verification code: {verificationCode}";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // user email confirmed, notify user
        public void SendEmailConfirmedEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "Email Confirmed";
            string emailBody = $"Congratulations {userName}, your email is now verified.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // send a verification code to user's email
        public void SendNewSessionVerificationEmail(string emailFrom, string emailTo, string userName, string verificationCode)
        {
            string emailSubject = "Please Confirm New Session";
            string emailBody = $"Greetings, please confirm new sign in by submitting this verification code: {verificationCode}";

            // user account successfully created, initiate email confirmation
            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // notify the user about account lockout
        public void SendAccountLockedOutEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "Account Locked Out";
            string emailBody = $"Dear {userName}, due to 3 unsuccessful attempts to sign in to your account, we have locked it out. You can try again in 30 minutes or click this link to reset your TOTP access.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // notify user about new session
        public void SendNewActiveSessionNotificationEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "New Active Session Started";
            string emailBody = $"Dear {userName}, this is to notify you of a new active session.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // notify user to complete account registration
        public void SendAccountNotRegisteredEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "SignIn Attempt Detected";
            string emailBody = $"Dear {userName}, we have detected a sign in attempt for your account. To log in, you need to finish registration.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
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
