using System;
using System.Threading.Tasks;
using System.Net;
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
using CoreIdentityServer.Internals.Constants.Account;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Constants.Storage;
using Newtonsoft.Json;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        private readonly IIdentityServerInteractionService InteractionService;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        public readonly RouteValueDictionary RootRoute;
        private bool ResourcesDisposed;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            IdentityService identityService,
            IIdentityServerInteractionService interactionService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory
        ) {
            UserManager = userManager;
            EmailService = emailService;
            IdentityService = identityService;
            InteractionService = interactionService;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            RootRoute = GenerateRedirectRouteValues("SignIn", "Authentication", "Access");
        }

        public async Task<object[]> ManageEmailChallenge(string returnUrl)
        {
            object[] result = await IdentityService.ManageEmailChallenge(RootRoute, returnUrl);

            return result;
        }

        public async Task<string> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
        {
            string redirectRoute = null;

            if (!ActionContext.ModelState.IsValid)
                return redirectRoute;

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
                
                redirectRoute = GenerateRouteUrl("SignIn", "Authentication", "Access");
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
                    redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        inputModel.ResendEmailRecordId,
                        UserActionContexts.SignInEmailChallenge,
                        null
                    );

                    // signin succeeded & returnUrl present in query string, redirect to returnUrl
                    if (redirectRoute != null && !string.IsNullOrWhiteSpace(inputModel.ReturnUrl))
                        redirectRoute = $"~{inputModel.ReturnUrl}";
                }
                else
                {
                    await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                }
            }

            return redirectRoute;
        }

        public object[] ManageSignIn(string returnUrl)
        {
            RouteValueDictionary redirectRouteValues = null;

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");

            SignInInputModel viewModel = string.IsNullOrWhiteSpace(returnUrl) ? null : new SignInInputModel { ReturnUrl = returnUrl };

            return GenerateArray(viewModel, redirectRouteValues);
        }

        public async Task<string> SignIn(SignInInputModel inputModel)
        {
            string redirectRoute = null;
            string redirectRouteQueryString = null;

            if (!string.IsNullOrWhiteSpace(inputModel.ReturnUrl))
            {
                string urlEncodedReturnUrl = WebUtility.UrlEncode(inputModel.ReturnUrl);
                redirectRouteQueryString = $"?ReturnUrl={urlEncodedReturnUrl}";
            }

            // check if there is a current user logged in, if so redirect to an authorized page
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
            {
                redirectRoute = GenerateRouteUrl("RegisterTOTPAccessSuccessful", "SignUp", "Enroll", redirectRouteQueryString);
                return redirectRoute;
            }

            // check if ModelState is valid, if not return null to return viewModel
            if (!ActionContext.ModelState.IsValid)
                return redirectRoute;

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
                
                redirectRoute = GenerateRouteUrl("Access", "Authentication", "SignIn", redirectRouteQueryString);
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
                        redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                            user,
                            null,
                            UserActionContexts.SignInTOTPChallenge,
                            null
                        );

                        // add query string since IdentityService.ManageTOTPChallengeSuccess method doesn't share this concern
                        redirectRoute = redirectRoute + redirectRouteQueryString;
                    }
                    else
                    {
                        await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                    }
                }
            }

            return redirectRoute;
        }

        public async Task<SignOutViewModel> ManageSignOut(string signOutId)
        {
            SignOutViewModel viewModel = new SignOutViewModel { SignOutId = signOutId, ShowSignOutPrompt = AccountOptions.ShowSignOutPrompt };

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            // user is not logged in, sign out without showing prompt
            if (!currentUserSignedIn)
            {
                viewModel.ShowSignOutPrompt = false;

                return viewModel;
            }

            LogoutRequest logoutContext = await InteractionService.GetLogoutContextAsync(signOutId);

            if (logoutContext != null && !logoutContext.ShowSignoutPrompt)
            {
                viewModel.ShowSignOutPrompt = false;
            }

            return viewModel;
        }

        public async Task<RouteValueDictionary> SignOut(SignOutInputModel inputModel)
        {
            SignedOutViewModel viewModel = null;

            LogoutRequest logoutContext = await InteractionService.GetLogoutContextAsync(inputModel.SignOutId);

            if (logoutContext != null)
            {
                viewModel = new SignedOutViewModel
                {
                    AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                    PostLogoutRedirectUri = logoutContext.PostLogoutRedirectUri,
                    ClientName = string.IsNullOrWhiteSpace(logoutContext.ClientName) ? logoutContext.ClientId : logoutContext.ClientName,
                    SignOutIFrameUrl = logoutContext.SignOutIFrameUrl,
                    SignOutId = inputModel.SignOutId
                };
            }

            // sign out user
            await IdentityService.SignOut();

            if (viewModel != null)
                TempData[TempDataKeys.SignedOutViewModel] = JsonConvert.SerializeObject(viewModel);

            return GenerateRedirectRouteValues("SignedOut", "Authentication", "Access");
        }

        public SignedOutViewModel ManageSignedOut()
        {
            SignedOutViewModel viewModel = null;

            bool signedOutViewModelExists = TempData.TryGetValue(
                TempDataKeys.SignedOutViewModel,
                out object signedOutViewModelTempData
            );

            if (signedOutViewModelExists)
                viewModel = JsonConvert.DeserializeObject<SignedOutViewModel>((string)signedOutViewModelTempData);

            return viewModel;
        }

        public async Task<string> ManageTOTPChallengeVerification(TOTPChallengeInputModel inputModel)
        {
            string redirectRoute = null;
            string targetRoute = GenerateRouteUrl("SignIn", "Authentication", "Access");

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return GenerateRouteUrl("SignIn", "Authentication", "Access");
            }

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRoute;
            }

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                redirectRoute = GenerateRouteUrl("SignIn", "Authentication", "Access");
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
                
                redirectRoute = GenerateRouteUrl("SignIn", "Authentication", "Access");
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
                    redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
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

            return redirectRoute;
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
