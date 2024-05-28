using System;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Roles;
using CoreIdentityServer.Internals.Constants.Storage;
using CoreIdentityServer.Internals.Services;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace CoreIdentityServer.Areas.Administration.Services
{
    public class RolesService : BaseService, IDisposable
    {
        private readonly RoleManager<IdentityRole> RoleManager;
        private IUrlHelper UrlHelper;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public RolesService(
            RoleManager<IdentityRole> roleManager,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IUrlHelperFactory urlHelperFactory
        ) : base(actionContextAccessor, tempDataDictionaryFactory)
        {
            RoleManager = roleManager;
            UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            RootRoute = GenerateRouteUrl("Index", "Roles", "Administration");
        }


        /// <summary>
        ///     public async Task<IndexViewModel> ManageIndex()
        ///     
        ///     Manages the Index action to show all roles.
        ///         The method lists all roles and returns them in a view model.
        /// </summary>
        /// <returns>View model containing all roles</returns>
        public async Task<IndexViewModel> ManageIndex()
        {
            IndexViewModel viewModel = new IndexViewModel();

            viewModel.Roles = await RoleManager.Roles.ProjectToType<RoleViewModel>().ToListAsync();

            return viewModel;
        }


        /// <summary>
        ///     public async Task<object[]> ManageDetails(string id)
        ///     
        ///     Manages the Details action to show role details.
        ///     
        ///     1. Fetches the role from the database using the id.
        ///         If the role is not found, an array of objects containing
        ///             null and the RootRoute is returned from the method.
        ///             
        ///     2. If the role is found, an array of objects containing the role
        ///         details and null is returned from the method. 
        /// </summary>
        /// <param name="id">Id of the role</param>
        /// <returns>
        ///     An array of objects containing
        ///         the role details and null
        ///             or,
        ///                 null and the RootRoute.
        /// </returns>
        public async Task<object[]> ManageDetails(string id)
        {
            IdentityRole role = await RoleManager.FindByIdAsync(id);

            if (role == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "Role not found.";

                return GenerateArray(null, RootRoute);
            }

            EditRoleInputModel roleInputModel = new EditRoleInputModel {
                Id = role.Id,
                Name = role.Name
            };

            return GenerateArray(roleInputModel, null);
        }


        /// <summary>
        ///     public async Task<string> ManageUpdate(EditRoleInputModel inputModel)
        ///     
        ///     Manages the Details action to update role details.
        ///     
        ///     1. Checks if the ModelState is valid. If not the method returns null.
        ///     
        ///     2. Fetches the role using the id in the input model. If the role is not
        ///         found, the method returns the RootRoute.
        ///         
        ///     3. If the role is found and it is the role of the Product Owner, the
        ///         method adds an error to the ModelState as the Product Owner role
        ///             cannot be updated. Then the method returns null.
        ///             
        ///     4. If the role name matches the one in the input model, the role
        ///         is already up to date and does not need any change. The method
        ///             returns a url for the role details page.
        ///             
        ///     5. If the role name doesn't match, the name is updated with that
        ///         of the input model. If the update fails, errors are added to
        ///             the ModelState and the method returns null.
        ///             
        ///     6. If the update succeeds, the method returns a url to the role
        ///         details page.
        /// </summary>
        /// <param name="inputModel">
        ///     Input model containing role details that need to be updated
        /// </param>
        /// <returns>A url to the details page of the updated role or null</returns>
        public async Task<string> ManageUpdate(EditRoleInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;

            IdentityRole role = await RoleManager.FindByIdAsync(inputModel.Id);

            if (role == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "Role not found.";

                return RootRoute;
            }
            else if (role.Name == "Product Owner")
            {
                ActionContext.ModelState.AddModelError(string.Empty, "Cannot update role 'Product Owner'");

                return null;
            }
            else if (role.Name == inputModel.Name)
            {
                TempData[TempDataKeys.SuccessMessage] = "Role updated.";

                return UrlHelper.Action("Details", "Roles", new { Area = "Administration", Id = inputModel.Id });
            }
            else
            {
                role.Name = inputModel.Name;

                IdentityResult updateRole = await RoleManager.UpdateAsync(role);

                if (!updateRole.Succeeded)
                {
                    // updating role failed, adding erros to ModelState
                    foreach (IdentityError error in updateRole.Errors)
                        ActionContext.ModelState.AddModelError(string.Empty, error.Description);

                    return null;
                }
                else
                {
                    TempData[TempDataKeys.SuccessMessage] = "Role updated.";

                    return UrlHelper.Action("Details", "Roles", new { Area = "Administration", Id = inputModel.Id });
                }
            }
        }


        /// <summary>
        ///     public async Task<string> ManageCreate(CreateRoleInputModel inputModel)
        ///     
        ///     Manages the Create action to create a role.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns null.
        ///     
        ///     2. Fetches the role using the name in the input model. If the role already
        ///         exists, an error message is added to the ModelState for the user. And
        ///             the method returns null.
        ///         
        ///     3. If the role doesn't exist, a new IdentityRole is created and saved to the
        ///         database. If the save fails, errors are added to the ModelState for the user.
        ///             And the method returns null.
        ///             
        ///     4. If the save succeeds, a url to the details page of the newly created role is
        ///         returned.
        /// </summary>
        /// <param name="inputModel">Input model containing the role creation data</param>
        /// <returns>Url to the details page of the new role or null</returns>
        public async Task<string> ManageCreate(CreateRoleInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;

            IdentityRole role = await RoleManager.FindByNameAsync(inputModel.Name);

            if (role != null)
            {
                ActionContext.ModelState.AddModelError(string.Empty, $"A role already exists with the name '{inputModel.Name}'");
            
                return null;
            }
            else
            {
                role = new IdentityRole {
                    Name = inputModel.Name
                };

                IdentityResult createRole = await RoleManager.CreateAsync(role);

                if (!createRole.Succeeded)
                {
                    // creating role failed, adding erros to ModelState
                    foreach (IdentityError error in createRole.Errors)
                        ActionContext.ModelState.AddModelError(string.Empty, error.Description);

                    return null;
                }
                else
                {
                    TempData[TempDataKeys.SuccessMessage] = "Role created.";

                    return UrlHelper.Action("Details", "Roles", new { Area = "Administration", Id = role.Id });
                }
            }
        }


        /// <summary>
        ///     public async Task<string> ManageDelete(DeleteRoleInputModel inputModel)
        ///     
        ///     Manages the Delete action to delete a role.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns a url
        ///         to the Error page.
        ///         
        ///     2. Fetches the role using the id in the input model. If the role is not
        ///         found, the method returns the RootRoute.
        ///         
        ///     3. If the role is found and it is the role of the Product Owner, an error
        ///         message is added for the user and the method returns the url to the
        ///             details page of that role. This is because the Product Owner
        ///                 role cannot be deleted.
        ///                 
        ///     4. If the role is any other, it is deleted using the RoleManager.DeleteAsync()
        ///         method. If the delete fails, all errors are added to the console. And an
        ///             error message is added for the user. Then the method returns the
        ///                 RootRoute.
        ///                 
        ///     5. In case the delete succeeds, the method returns a RootRoute.
        /// </summary>
        /// <param name="inputModel">Input model containing id of the role to delete</param>
        /// <returns>
        ///     A url to the Error page or the RootRoute or the Details page depending on the scenario
        /// </returns>
        public async Task<string> ManageDelete(DeleteRoleInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return GenerateRouteUrl("Error", "Correspondence", "ClientServices");

            IdentityRole role = await RoleManager.FindByIdAsync(inputModel.Id);

            if (role == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "Role not found.";

                return RootRoute;
            }
            else if (role.Name == "Product Owner")
            {
                TempData[TempDataKeys.ErrorMessage] = "Cannot delete role 'Product Owner'.";

                return UrlHelper.Action("Details", "Roles", new { Area = "Administration", Id = role.Id });
            }
            else
            {
                IdentityResult deleteRole = await RoleManager.DeleteAsync(role);

                if (!deleteRole.Succeeded)
                {
                    // deleting role failed, logging errors
                    foreach (IdentityError error in deleteRole.Errors)
                        Console.WriteLine($"Could not delete Role: {error.Description}");

                    TempData[TempDataKeys.ErrorMessage] = "Error deleting role.";
                }
                else
                {
                    TempData[TempDataKeys.SuccessMessage] = "Role deleted.";
                }

                return RootRoute;
            }
        }


        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            RoleManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}