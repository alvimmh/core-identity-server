using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Internals.Constants.UserActions;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Storage;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

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
            RootRoute = GenerateRouteUrl("Prompt", "ResetTOTPAccess", "Access");
        }

        public async Task<string> InitiateEmailChallenge(InitiateEmailChallengeInputModel inputModel)
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
                // user doesn't exist
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
                string verificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

                // send email with verification code & set the redirect url
                string resendEmailRecordId = await EmailService.SendResetTOTPAccessVerificationEmail(
                    AutomatedEmails.NoReply,
                    user.Email,
                    user.UserName,
                    verificationCode
                );

                // clear all unnecessary tempdata
                TempData.Clear();

                if (!currentUserSignedIn)
                    TempData[TempDataKeys.UserEmail] = user.Email;

                TempData[TempDataKeys.ResendEmailRecordId] = resendEmailRecordId;

                redirectRoute = GenerateRouteUrl("EmailChallenge", "ResetTOTPAccess", "Access");
            }

            return redirectRoute;
        }

        public async Task<object[]> ManageEmailChallenge()
        {
            object[] result = await IdentityService.ManageEmailChallenge(RootRoute);

            return result;
        }

        public async Task<string> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
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
                        UserActionContexts.ResetTOTPAccessEmailChallenge,
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
