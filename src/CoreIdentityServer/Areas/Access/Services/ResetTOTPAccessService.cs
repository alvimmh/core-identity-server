using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Internals.Constants.UserActions;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Storage;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class ResetTOTPAccessService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        private ITempDataDictionary TempData;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public ResetTOTPAccessService(
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory 
        ) {
            UserManager = userManager;
            EmailService = emailService;
            IdentityService = identityService;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            RootRoute = GenerateRouteUrl("ManageAuthenticator", "ResetTOTPAccess", "Access");
        }

        public async Task<ManageAuthenticatorViewModel> ManageAuthenticator()
        {
            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
                return null;

            int recoveryCodesLeft = await UserManager.CountRecoveryCodesAsync(user);

            return new ManageAuthenticatorViewModel { RecoveryCodesLeft = recoveryCodesLeft };
        }

        public async Task<string> InitiateTOTPAccessRecoveryChallenge(InitiateTOTPAccessRecoveryChallengeInputModel inputModel)
        {
            string redirectRoute = RootRoute;
            ApplicationUser user = null;
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user == null)
                    return redirectRoute;
            }
            else if (!ActionContext.ModelState.IsValid)
            {
                return redirectRoute;
            }

            user = user ?? await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null || !user.EmailConfirmed)
            {
                // user doesn't exist or user email is not confirmed
                return redirectRoute;
            }
            else if (!user.AccountRegistered)
            {
                // user exists but did not complete account registration, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, inputModel.Email, user.UserName);

                return redirectRoute;
            }
            else if (user.AccountRegistered)
            {
                // clear all unnecessary tempdata
                TempData.Clear();

                if (!currentUserSignedIn)
                    TempData[TempDataKeys.UserEmail] = user.Email;

                redirectRoute = GenerateRouteUrl("RecoverTOTPAccessChallenge", "ResetTOTPAccess", "Access");
            }

            return redirectRoute;
        }

        public async Task<object[]> ManageTOTPAccessRecoveryChallenge()
        {
            object[] result = await IdentityService.ManageTOTPAccessRecoveryChallenge(RootRoute);

            return result;
        }

        public async Task<string> ManageTOTPAccessRecoveryChallengeVerification(TOTPAccessRecoveryChallengeInputModel inputModel)
        {
            string redirectRoute = null;

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRoute;
            }

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                redirectRoute = RootRoute;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
                
                redirectRoute = RootRoute;
            }
            else if (user.EmailConfirmed && user.AccountRegistered)
            {
                // user exists with registered account and confirmed email, so user trying to reset TOTP access

                bool totpAccessRecoveryCodeVerified = await IdentityService.VerifyTOTPAccessRecoveryCode(
                    user, inputModel.VerificationCode
                );

                if (totpAccessRecoveryCodeVerified)
                {
                    redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        null,
                        UserActionContexts.ResetTOTPAccessRecoveryChallenge,
                        null
                    );
                }
                else
                {
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid recovery code");
                }
            }

            return redirectRoute;
        }

        public async Task<object[]> ManageResetTOTPAccessRecoveryCodes()
        {
            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user != null)
            {
                ResetTOTPAccessRecoveryCodesInputModel viewModel = new ResetTOTPAccessRecoveryCodesInputModel() { Id = user.Id };
            
                return GenerateArray(viewModel, null);
            }
            
            return GenerateArray(null, RootRoute);
        }

        public async Task<string> ResetTOTPAccessRecoveryCodes(ResetTOTPAccessRecoveryCodesInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return RootRoute;

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user != null && user.Id == inputModel.Id)
            {
                IEnumerable<string> userTOTPRecoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);

                return GenerateRouteUrl("RegisterTOTPAccessSuccessful", "SignUp", "Enroll", "resetaccess=true");
            }

            return RootRoute;
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
