using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Areas.Vault.Models.Profile;
using CoreIdentityServer.Internals.Constants.Administration;

namespace CoreIdentityServer.Areas.Vault.Services
{
    public class ProfileService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private IdentityService IdentityService;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public ProfileService(
            UserManager<ApplicationUser> userManager,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor
        ) : base(actionContextAccessor) {
            UserManager = userManager;
            IdentityService = identityService;
            RootRoute = GenerateRouteUrl("Index", "Profile", "Vault");
        }


        /// <summary>
        ///     public async Task<string> GetUserEmail(UserEmailInputModel inputModel)
        ///     
        ///     Manages the UserEmail POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns null.
        ///     
        ///     2. Fetches the user using the method UserManager.FindByIdAsync(). If not found,
        ///         the method returns null.
        ///         
        ///     3. Checks if the user is a Product Owner. If not, the method returns the user's
        ///         email address as a string. Otherwise, the method returns null.
        /// </summary>
        /// <param name="inputModel">The input model containing the user's id, the requesting client's id and secret</param>
        /// <returns>The user's email as a string or null</returns>
        public async Task<string> GetUserEmail(UserEmailInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;
            
            ApplicationUser user = await UserManager.FindByIdAsync(inputModel.user_id);

            if (user == null)
                return null;

            bool isUserProductOwner = await UserManager.IsInRoleAsync(user, AuthorizedRoles.ProductOwner);

            if (isUserProductOwner)
                return null;

            return user.Email;
        }


        /// <summary>
        ///     public async Task<object[]> ManageUserProfile()
        ///     
        ///     Manages the Index GET action to show the user's profile.
        ///     
        ///     1. Checks if the user is signed in. If not, the method returns an array
        ///         of objects containing null and the RootRoute - the route to the Index Page.
        ///         
        ///     2. Fetches the user using the UserManager.GetUserAsync() method. If the
        ///         user is not found, the method returns an array of objects containing
        ///             null and the RootRoute.
        ///             
        ///     3. If the user is found, an input model is created with the user's first and last
        ///         name, and the user's email address. Finally, the method returns an array
        ///             of objects containing the input model and null.
        /// </summary>
        /// <returns>
        ///     An array of objects containing the
        ///         the created input model
        ///             or,
        ///                 null.
        /// </returns>
        public async Task<object[]> ManageUserProfile()
        {
            // although ProfileController uses Authorize attribute so users won't reach here if not authorized
            // but this is a separate service which may not always be used in ProfileController
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return GenerateArray(null, RootRoute);
            }

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
                return GenerateArray(null, RootRoute);

            UserProfileInputModel model = new UserProfileInputModel() {
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            model.SetEmail(user.Email);

            return GenerateArray(model, null);
        }


        /// <summary>
        ///     public async Task<string> UpdateUserProfile(UserProfileInputModel inputModel)
        ///     
        ///     Manages the Index POST action to update the user's profile.
        ///     
        ///     1. Checks if the user is signed in. If not, the method returns the RootRoute.
        ///     
        ///     2. The user is fetched using the method UserManager.GetUserAsync(). If the user
        ///         is not found, the method then returns the RootRoute. If the user is found,
        ///             the method continues.
        ///
        ///     2. Checks if the ModelState is valid.
        ///        
        ///        If it is not valid, the user's email is set in the input model using a setter.
        ///         This is a security feature which prevents the user from overriding the email address.
        ///             And the method then returns null which will take the user back to the same page.
        ///                 
        ///     3. In case the ModelState was valid, the user is updated from the input model
        ///         data and using the method UserManager.UpdateUser().
        ///         
        ///     4. If the update failed, the errors are printed to the console. An error is
        ///         added to the ModelState as well for the user. And the method returns null.
        ///         
        ///     5. If the update succeeded, the method returns the RootRoute.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model containing the information to be changed about the user
        /// </param>
        /// <returns>The route to redirect the application or null</returns>
        public async Task<string> UpdateUserProfile(UserProfileInputModel inputModel)
        {
            // although ProfileController uses Authorize attribute so users won't reach here if not authorized
            // but this is a separate service which may not always be used in ProfileController
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
                return RootRoute;

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
                return RootRoute;

            if (!ActionContext.ModelState.IsValid)
            {
                inputModel.SetEmail(user.Email);

                return null;
            }

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

                return null;
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
