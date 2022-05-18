using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Storage;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Areas.Access.Models.MFA;
using CoreIdentityServer.Internals.Constants.Emails;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class MFAService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        private ITempDataDictionary TempData;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public MFAService(
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
            RootRoute = GenerateRouteUrl("Dashboard", "Pages", "General");
        }

        public async Task<object[]> ManageEmailAuthentication()
        {
            object[] result = GenerateArray(null, RootRoute);

            ApplicationUser user = null;
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user == null)
                    return result;

                bool twoFactorAuthenticationEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

                ManageEmailAuthenticationViewModel viewModel = new ManageEmailAuthenticationViewModel();
                viewModel.SetEmailAuthenticationEnabled(twoFactorAuthenticationEnabled);

                return GenerateArray(viewModel, null);
            }
            else
            {
                return result;
            }
        }

        public async Task<string> SetEmailAuthentication(SetEmailAuthenticationInputModel inputModel)
        {
            string redirectRoute = GenerateRouteUrl("ManageEmailAuthentication", "MFA", "Access");
            ApplicationUser user = null;

            if (!ActionContext.ModelState.IsValid)
                return redirectRoute;

            bool enableEmailAuthentication = Convert.ToBoolean(inputModel.Enable);
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user == null)
                    return RootRoute;

                bool twoFactorAuthenticationEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

                if (twoFactorAuthenticationEnabled == enableEmailAuthentication)
                    return redirectRoute;
                
                string userChoice = enableEmailAuthentication ? "enable" : "disable";

                bool userIsProductOwner = await UserManager.IsInRoleAsync(user, AuthorizedRoles.ProductOwner);

                if (userIsProductOwner && !enableEmailAuthentication)
                {
                    TempData[TempDataKeys.ErrorMessage] = $"Product Owner cannot {userChoice} two factor authentication.";

                    return redirectRoute;
                }

                IdentityResult setTwoFactorEnabled = await UserManager.SetTwoFactorEnabledAsync(user, enableEmailAuthentication);

                if (!setTwoFactorEnabled.Succeeded)
                {
                    // setting email authentication to required value failed, adding errors to show
                    foreach (IdentityError error in setTwoFactorEnabled.Errors)
                        Console.WriteLine(error.Description);
                    
                    TempData[TempDataKeys.ErrorMessage] = $"Could not {userChoice} two factor authentication. Please try again later.";
                
                    return redirectRoute;
                }

                // refresh user sign in to update claims
                await IdentityService.RefreshUserSignIn(user);

                if (enableEmailAuthentication)
                {
                    TempData[TempDataKeys.SuccessMessage] = $"Two factor authentication {userChoice}d successfully.";
                }
                else
                {
                    TempData[TempDataKeys.ErrorMessage] = $"Two factor authentication {userChoice}d.";
                }

                // send email about new email authentication status to user
                await EmailService.SendNewTwoFactorAuthenticationStatusEmail(
                    AutomatedEmails.NoReply, user.Email, user.UserName, enableEmailAuthentication
                );

                return redirectRoute;
            }
            else
            {
                return RootRoute;
            }
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
