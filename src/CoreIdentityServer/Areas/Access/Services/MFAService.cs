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


        /// <summary>
        ///     public async Task<object[]> ManageEmailAuthentication()
        ///     
        ///     Manages the ManageEmailAuthentication GET action.
        ///     
        ///     1. Checks if the current user is signed in. If not, returns an array of
        ///         objects containing null and the RootRoute - the Dashboard page.
        ///         
        ///     2. If the current user is signed in, fetches the user using the
        ///         UserManager.GetUserAsync() method. If the user was not found, the
        ///             method returns an array of objects containing null and the RootRoute.
        ///             
        ///     3. If the user was found, checks if two factor authentication is enabled for
        ///         the user using the method UserManager.GetTwoFactorEnabledAsync() method.
        ///         
        ///     4. Creates a view model containing a boolean which indicates if 2FA is enabled
        ///         for the user. Finally, the method returns an array of objects containing
        ///             the view model and null.
        /// </summary>
        /// <returns>
        ///     An array of objects containing
        ///         the view model and null
        ///             or,
        ///                 null and a redirect route.
        /// </returns>
        public async Task<object[]> ManageEmailAuthentication()
        {
            object[] result = GenerateArray(null, RootRoute);

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return GenerateArray(null, RootRoute);
            }
            else
            {
                ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user == null)
                    return GenerateArray(null, RootRoute);

                bool twoFactorAuthenticationEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

                ManageEmailAuthenticationViewModel viewModel = new ManageEmailAuthenticationViewModel();
                viewModel.SetEmailAuthenticationEnabled(twoFactorAuthenticationEnabled);

                return GenerateArray(viewModel, null);
            }
        }


        /// <summary>
        ///     public async Task<string> SetEmailAuthentication(
        ///         SetEmailAuthenticationInputModel inputModel
        ///     )
        ///     
        ///     Manages the ManageEmailAuthentication POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, returns a redirect route
        ///         to the Manage Email Authentication page.
        ///         
        ///     2. Checks if the current user is signed in using the
        ///         IdentityService.CheckActiveSession() method. If not, the method returns the
        ///             RootRoute.
        ///             
        ///     3. If the user is signed in, fetches the user using the UserManager.GetUserAsync()
        ///         method. If the user was not found, the method returns the RootRoute.
        ///         
        ///     4. Checks if two factor authentication is enabled for the user. In this application,
        ///         2FA means a user has turned on email based authentication in addition to the basic
        ///             TOTP authenticator based default option. If 2FA status is the same as the one
        ///                 the user wants to change to, then the method returns a redirect route to
        ///                     the Manage Email Authentication page. Otherwise, the method continues.
        ///     
        ///     5. Checks if the user is the Product Owner of this application. If the user is, and
        ///         wants to disable 2FA, the method sets an error message in the TempData for the
        ///             user and returns a redirect route to the Manage Email Authentication page.
        ///                 This is because Product Owner cannot disable 2FA in this application.
        ///                 
        ///     6. If the user is not the Product Owner, the method updates the user's 2FA preference
        ///         using the UserManager.SetTwoFactorEnabledAsync() method. If the update failed,
        ///             all errors are printed to the console and an error message is set in the
        ///                 TempData for the user. Then the method returns a redirect route to the
        ///                     Manage Email Authentication page.
        ///                     
        ///     7. If the update succeeded, the user's session is refreshed using the
        ///         IdentityService.RefreshUserSignIn() method. Additionally, a notification regarding
        ///             the 2FA status change is set in the TempData for the user. An email is also
        ///                 sent to the user notifying them about the 2FA status change. Finally, the
        ///                     method returns a redirect route to the Manage Email Authentication page.
        /// </summary>
        /// <param name="inputModel">The input model containing the user's 2FA status preference</param>
        /// <returns>A route to redirect the application</returns>
        public async Task<string> SetEmailAuthentication(SetEmailAuthenticationInputModel inputModel)
        {
            ApplicationUser user = null;

            if (!ActionContext.ModelState.IsValid)
            {
                string redirectRoute = GenerateRouteUrl("ManageEmailAuthentication", "MFA", "Access");

                return redirectRoute;
            }

            bool enableEmailAuthentication = Convert.ToBoolean(inputModel.Enable);
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return RootRoute;
            }
            else
            {
                string redirectRoute = GenerateRouteUrl("ManageEmailAuthentication", "MFA", "Access");

                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user == null)
                    return RootRoute;

                bool twoFactorAuthenticationEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

                if (twoFactorAuthenticationEnabled == enableEmailAuthentication)
                {
                    return redirectRoute;
                }
                
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
