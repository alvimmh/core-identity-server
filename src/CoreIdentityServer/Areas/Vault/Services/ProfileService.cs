using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Areas.Vault.Models.Profile;

namespace CoreIdentityServer.Areas.Vault.Services
{
    public class ProfileService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        public readonly RouteValueDictionary RootRoute;
        private bool ResourcesDisposed;

        public ProfileService(
            UserManager<ApplicationUser> userManager,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor
        ) {
            UserManager = userManager;
            IdentityService = identityService;
            ActionContext = actionContextAccessor.ActionContext;
            RootRoute = GenerateRedirectRouteValues("Index", "Profile", "Vault");
        }

        public async Task<object[]> ManageUserProfile()
        {
            UserProfileInputModel model = null;
            RouteValueDictionary redirectRouteValues = RootRoute;
            object[] result = GenerateArray(model, redirectRouteValues);

            // although ProfileController uses Authorize attribute so users won't reach here if not authorized
            // but this is a separate service which may not always be used in ProfileController
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return result;
            }

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
                return result;

            model = new UserProfileInputModel() {
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            model.SetEmail(user.Email);

            return GenerateArray(model, redirectRouteValues);
        }

        public async Task<RouteValueDictionary> UpdateUserProfile(UserProfileInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;
          
            // although ProfileController uses Authorize attribute so users won't reach here if not authorized
            // but this is a separate service which may not always be used in ProfileController
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
                return RootRoute;

            if (!ActionContext.ModelState.IsValid)
            {
                ApplicationUser currentUser = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (currentUser == null)
                    return RootRoute;
                
                inputModel.SetEmail(currentUser.Email);

                return redirectRouteValues;
            }

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
                return redirectRouteValues;

            user.FirstName = inputModel.FirstName;
            user.LastName = inputModel.LastName;

            IdentityResult updateUser = await UserManager.UpdateAsync(user);

            if (!updateUser.Succeeded)
            {
                Console.WriteLine($"Error updating user");

                // add errors to ModelState
                foreach (IdentityError error in updateUser.Errors)
                    Console.WriteLine(error.Description);
                
                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                return redirectRouteValues;
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