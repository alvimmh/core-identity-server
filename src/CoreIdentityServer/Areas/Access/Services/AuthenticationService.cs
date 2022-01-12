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
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Constants.UserActions;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        public readonly RouteValueDictionary RootRoute;
        private bool ResourcesDisposed;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor
        ) {
            UserManager = userManager;
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
            RouteValueDictionary redirectRouteValues = await IdentityService.VerifyEmailChallenge(
                inputModel,
                RootRoute,
                null,
                CustomTokenOptions.GenericTOTPTokenProvider,
                UserActionContexts.SignInEmailChallenge
            );

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

            redirectRouteValues = await IdentityService.VerifyTOTPChallenge(
                inputModel.Email,
                inputModel.TOTPCode,
                RootRoute,
                null,
                TokenOptions.DefaultAuthenticatorProvider,
                UserActionContexts.SignInTOTPChallenge,
                null
            );

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
                return RootRoute;
            }

            redirectRouteValues = await IdentityService.VerifyTOTPChallenge(
                user.Email,
                inputModel.VerificationCode,
                RootRoute,
                targetRoute,
                TokenOptions.DefaultAuthenticatorProvider,
                UserActionContexts.TOTPChallenge,
                null
            );

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
