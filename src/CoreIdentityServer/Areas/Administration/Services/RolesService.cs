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
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        private IUrlHelper UrlHelper;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public RolesService(
            RoleManager<IdentityRole> roleManager,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IUrlHelperFactory urlHelperFactory
        ) {
            RoleManager = roleManager;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            RootRoute = GenerateRouteUrl("Index", "Roles", "Administration");
        }

        public async Task<IndexViewModel> ManageIndex()
        {
            IndexViewModel viewModel = new IndexViewModel();

            viewModel.Roles = await RoleManager.Roles.ProjectToType<RoleViewModel>().ToListAsync();

            return viewModel;
        }

        public async Task<object[]> ManageDetails(string id)
        {
            IdentityRole role = await RoleManager.FindByIdAsync(id);

            EditRoleInputModel roleInputModel = role == null ? null : new EditRoleInputModel {
                Id = role.Id,
                Name = role.Name
            };

            return GenerateArray(roleInputModel, RootRoute);
        }

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

        public async Task<string> ManageCreate(CreateRoleInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;

            IdentityRole role = await RoleManager.FindByNameAsync(inputModel.Name);

            if (role == null)
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
            else
            {
                ActionContext.ModelState.AddModelError(string.Empty, $"A role already exists with the name '{inputModel.Name}'");
            
                return null;
            }
        }

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