using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Internals.Constants.UserActions;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class ResetTOTPAccessService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        public readonly RouteValueDictionary RootRoute;
        private bool ResourcesDisposed;

        public ResetTOTPAccessService(
            UserManager<ApplicationUser> userManager,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor
        ) {
            UserManager = userManager;
            IdentityService = identityService;
            ActionContext = actionContextAccessor.ActionContext;
            RootRoute = GenerateRedirectRouteValues("Prompt", "ResetTOTPAccess", "Access");
        }

        public async Task<RouteValueDictionary> InitiateEmailChallenge(InitiateEmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = RootRoute;
            ApplicationUser user = null;
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user == null)
                    return redirectRouteValues;
            }
            else if (!ActionContext.ModelState.IsValid)
            {
                return redirectRouteValues;
            }

            user = user ?? await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null || !user.EmailConfirmed)
            {
                // user doesn't exist
                return redirectRouteValues;
            }
            else if (!user.AccountRegistered)
            {
                // user exists but did not complete account registration, send email to complete registration
                IdentityService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, inputModel.Email, user.UserName);

                return redirectRouteValues;
            }
            else if (user.AccountRegistered)
            {
                string verificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

                // send email with verification code & set redirectRouteValues
                IdentityService.SendResetTOTPAccessVerificationEmail(AutomatedEmails.NoReply, user.Email, user.UserName, verificationCode);

                // set email value so controller can save this to tempdata
                inputModel.Email = user.Email;

                redirectRouteValues = GenerateRedirectRouteValues("EmailChallenge", "ResetTOTPAccess", "Access");
            }

            return redirectRouteValues;
        }

        public async Task<object[]> ManageEmailChallenge(ITempDataDictionary tempData)
        {
            object[] result = await IdentityService.ManageEmailChallenge(tempData, RootRoute);

            return result;
        }

        public async Task<RouteValueDictionary> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await IdentityService.VerifyEmailChallenge(
                inputModel,
                RootRoute,
                null,
                CustomTokenOptions.GenericTOTPTokenProvider,
                UserActionContexts.ResetTOTPAccessEmailChallenge
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
