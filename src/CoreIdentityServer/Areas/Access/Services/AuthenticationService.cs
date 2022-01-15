using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Access.Models.Authentication;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Constants.UserActions;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Tokens;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        public readonly RouteValueDictionary RootRoute;
        private bool ResourcesDisposed;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor
        ) {
            UserManager = userManager;
            EmailService = emailService;
            IdentityService = identityService;
            ActionContext = actionContextAccessor.ActionContext;
            RootRoute = GenerateRedirectRouteValues("SignIn", "Authentication", "Access");
        }

        public async Task<object[]> ManageEmailChallenge()
        {
            object[] result = await IdentityService.ManageEmailChallenge(RootRoute);

            return result;
        }

        public async Task<RouteValueDictionary> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;

            if (!ActionContext.ModelState.IsValid)
                return redirectRouteValues;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, but don't reveal to end user
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
                
                redirectRouteValues = RootRoute;
            }
            else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
            {
                // user exists with unregistered account and unconfirmed email, so user is signing up
                // or
                // user exists with registered account and confirmed email, so user is either signing in or trying to reset TOTP access

                bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                    user,
                    CustomTokenOptions.GenericTOTPTokenProvider,
                    inputModel.VerificationCode
                );

                if (totpCodeVerified)
                {
                    redirectRouteValues = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        inputModel.ResendEmailRecordId,
                        UserActionContexts.SignInEmailChallenge,
                        null
                    );
                }
                else
                {
                    await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                }
            }

            return redirectRouteValues;
        }

        public RouteValueDictionary ManageSignIn()
        {
            RouteValueDictionary redirectRouteValues = null;

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");

            return redirectRouteValues;
        }

        public async Task<RouteValueDictionary> SignIn(SignInInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;
            
            // check if there is a current user logged in, if so redirect to an authorized page
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
            {
                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
                return redirectRouteValues;
            }

            // check if ModelState is valid, if not return ViewModel
            if (!ActionContext.ModelState.IsValid)
                return redirectRouteValues;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, but don't reveal to end user
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
                
                redirectRouteValues = RootRoute;
            }
            else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
            {
                // user exists with unregistered account and unconfirmed email, so user is signing up
                // or
                // user exists with registered account and confirmed email, so user is either signing in or trying to reset TOTP access

                if (user.RequiresAuthenticatorReset)
                {
                    await EmailService.SendResetTOTPAccessReminderEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                }
                else
                {
                    bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                        user,
                        TokenOptions.DefaultAuthenticatorProvider,
                        inputModel.TOTPCode
                    );

                    if (totpCodeVerified)
                    {
                        redirectRouteValues = await IdentityService.ManageTOTPChallengeSuccess(
                            user,
                            null,
                            UserActionContexts.SignInTOTPChallenge,
                            null
                        );
                    }
                    else
                    {
                        await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                    }
                }
            }

            return redirectRouteValues;
        }

        public async Task<RouteValueDictionary> SignOut()
        {
            // sign out user
            await IdentityService.SignOut();

            return RootRoute;
        }

        public async Task<RouteValueDictionary> ManageTOTPChallengeVerification(TOTPChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;
            RouteValueDictionary targetRoute = RootRoute;

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return RootRoute;
            }

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRouteValues;
            }

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                redirectRouteValues = RootRoute;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
                
                redirectRouteValues = RootRoute;
            }
            else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
            {
                // user exists with unregistered account and unconfirmed email, so user is signing up
                // or
                // user exists with registered account and confirmed email, so user is either signing in or trying to reset TOTP access

                bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                    user,
                    TokenOptions.DefaultAuthenticatorProvider,
                    inputModel.VerificationCode
                );

                if (totpCodeVerified)
                {
                    redirectRouteValues = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        null,
                        UserActionContexts.TOTPChallenge,
                        null
                    );
                }
                else
                {
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                }
            }

            return redirectRouteValues;
        }

        // clean up to be done by DI
        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}
