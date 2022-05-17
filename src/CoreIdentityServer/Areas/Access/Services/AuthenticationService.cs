using System;
using System.Threading.Tasks;
using System.Web;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Access.Models.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Constants.UserActions;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Constants.Account;
using CoreIdentityServer.Internals.Constants.Storage;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using CoreIdentityServer.Internals.Authorization.Handlers;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private readonly IConfiguration Configuration;
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        private readonly IIdentityServerInteractionService InteractionService;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        private readonly RouteEndpointService RouteEndpointService;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public AuthenticationService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            IdentityService identityService,
            IIdentityServerInteractionService interactionService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            RouteEndpointService routeEndpointService
        ) {
            Configuration = configuration;
            UserManager = userManager;
            EmailService = emailService;
            IdentityService = identityService;
            InteractionService = interactionService;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            RouteEndpointService = routeEndpointService;
            RootRoute = GenerateRouteUrl("SignIn", "Authentication", "Access");
        }

        public string ManageAccessDenied(string returnUrl)
        {
            bool returnUrlAvailable = !string.IsNullOrWhiteSpace(returnUrl);

            if (returnUrlAvailable)
            {
                bool returnUrlRequiresTOTPChallenge = RouteEndpointService.EndpointRoutesRequiringTOTPChallenge.Contains(returnUrl.ToLower());
                bool userHasTOTPAuthorization = returnUrlRequiresTOTPChallenge ? TOTPChallengeHandler.IsUserAuthorized(ActionContext.HttpContext.User) : false;

                if (returnUrlRequiresTOTPChallenge && !userHasTOTPAuthorization)
                {
                    string encodedReturnUrl = HttpUtility.UrlEncode(returnUrl.ToLower());

                    string totpChallengeRedirectRoute = GenerateRouteUrl("totpchallenge", "authentication", "access", $"returnurl={encodedReturnUrl}");

                    return totpChallengeRedirectRoute;
                }
            }

            return null;
        }

        public async Task<object[]> ManageEmailChallenge(string returnUrl)
        {
            string emailChallengeReturnUrl = IsValidReturnUrl(returnUrl, InteractionService, RouteEndpointService.EndpointRoutes) ? returnUrl : null;

            object[] result = await IdentityService.ManageEmailChallenge(RootRoute, emailChallengeReturnUrl);

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
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
            }
            else
            {
                bool userCanSignIn = await IdentityService.VerifySignInPrerequisites(user);

                if (userCanSignIn)
                {
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
                        if (redirectRoute != null && IsValidReturnUrl(inputModel.ReturnUrl, InteractionService, RouteEndpointService.EndpointRoutes))
                            redirectRoute = $"~{inputModel.ReturnUrl}";
                    }
                    else
                    {
                        await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                    }
                }
                else
                {
                    // add generic error and return ViewModel
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                }
            }

            return redirectRoute;
        }

        public SignInInputModel ManageSignIn(string returnUrl)
        {
            string signInReturnUrl = IsValidReturnUrl(returnUrl, InteractionService, RouteEndpointService.EndpointRoutes) ? returnUrl : null;

            SignInInputModel viewModel = new SignInInputModel { ReturnUrl = signInReturnUrl };

            return viewModel;
        }

        public async Task<string> SignIn(SignInInputModel inputModel)
        {
            string redirectRoute = null;
            string redirectRouteQueryString = null;

            if (IsValidReturnUrl(inputModel.ReturnUrl, InteractionService, RouteEndpointService.EndpointRoutes))
                redirectRouteQueryString = $"returnurl={HttpUtility.UrlEncode(inputModel.ReturnUrl)}";

            // check if ModelState is valid, if not return null to return viewModel
            if (!ActionContext.ModelState.IsValid)
                return redirectRoute;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, but don't reveal to end user
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
            }
            else
            {
                bool userCanSignIn = await IdentityService.VerifySignInPrerequisites(user);

                if (userCanSignIn)
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
                        redirectRoute = GenerateRouteUrl(redirectRoute, redirectRouteQueryString);    
                    }
                    else
                    {
                        await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
                    }
                }
                else
                {
                    // add generic error and return ViewModel
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
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

        public async Task<string> SignOut(SignOutInputModel inputModel)
        {
            SignedOutViewModel viewModel = null;

            LogoutRequest logoutContext = await InteractionService.GetLogoutContextAsync(inputModel.SignOutId);

            if (logoutContext != null)
            {
                viewModel = new SignedOutViewModel
                {
                    AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                    PostLogoutRedirectUri = logoutContext.PostLogoutRedirectUri ?? GenerateAbsoluteLocalUrl("SignIn", "Authentication", "Access", Configuration),
                    ClientName = string.IsNullOrWhiteSpace(logoutContext.ClientName) ? logoutContext.ClientId : logoutContext.ClientName,
                    SignOutIFrameUrl = logoutContext.SignOutIFrameUrl,
                    SignOutId = inputModel.SignOutId
                };
            }

            // sign out user
            await IdentityService.SignOut();

            if (viewModel != null)
                TempData[TempDataKeys.SignedOutViewModel] = JsonConvert.SerializeObject(viewModel);

            return GenerateRouteUrl("SignedOut", "Authentication", "Access");
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

        public TOTPChallengeInputModel ManageTOTPChallenge(string returnUrl)
        {
            string TOTPChallengeReturnUrl = IsValidReturnUrl(returnUrl, InteractionService, RouteEndpointService.EndpointRoutes) ? returnUrl : null;

            return new TOTPChallengeInputModel { ReturnUrl = TOTPChallengeReturnUrl };
        }

        public async Task<string> ManageTOTPChallengeVerification(TOTPChallengeInputModel inputModel)
        {
            string redirectRoute = null;

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return RootRoute;
            }

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRoute;
            }

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                redirectRoute = RootRoute;
            }
            else
            {
                bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                    user,
                    TokenOptions.DefaultAuthenticatorProvider,
                    inputModel.VerificationCode
                );

                if (totpCodeVerified)
                {
                    string targetRoute = null;

                    if (IsValidReturnUrl(inputModel.ReturnUrl, InteractionService, RouteEndpointService.EndpointRoutes))
                    {
                        targetRoute = $"~{inputModel.ReturnUrl}";
                    }
                    else
                    {
                        targetRoute = RootRoute;
                    }

                    redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        null,
                        UserActionContexts.TOTPChallenge,
                        targetRoute
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
