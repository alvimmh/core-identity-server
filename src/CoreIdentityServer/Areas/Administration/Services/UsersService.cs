using System;
using System.Linq;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Users;
using CoreIdentityServer.Internals.Constants.Administration;
using CoreIdentityServer.Internals.Constants.Storage;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoreIdentityServer.Areas.Administration.Services
{
    public class UsersService : BaseService, IDisposable
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        private IUrlHelper UrlHelper;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public UsersService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IUrlHelperFactory urlHelperFactory
        ) {
            DbContext = dbContext;
            UserManager = userManager;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(actionContextAccessor.ActionContext.HttpContext);
            UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            RootRoute = GenerateRouteUrl("Index", "Users", "Administration");
        }

        public async Task<IndexViewModel> ManageIndex(string page)
        {
            IndexViewModel viewModel = new IndexViewModel();

            int totalUsers = await UserManager.Users.CountAsync();
            
            double totalPages = (double)totalUsers / viewModel.ResultsInPage;
            viewModel.TotalPages = (int)Math.Ceiling(totalPages);

            int skipRecords = 0;

            bool isQueryStringInputValid = Int32.TryParse(page, out int currentPage);

            if (isQueryStringInputValid && currentPage > 0 && currentPage <= viewModel.TotalPages)
            {
                viewModel.CurrentPage = currentPage;
                int lastPage = currentPage - 1;
                skipRecords = lastPage * viewModel.ResultsInPage;
            }
            else
            {
                viewModel.CurrentPage = 1;
                skipRecords = 0;
            }

            viewModel.Users = await UserManager.Users
                                                .OrderBy(item => item.CreatedAt)
                                                .Skip(skipRecords)
                                                .Take(viewModel.ResultsInPage)
                                                .ProjectToType<UserViewModel>()
                                                .ToListAsync();

            return viewModel;
        }

        public async Task<object[]> ManageDetails(string userId)
        {
            object[] result = GenerateArray(null, RootRoute);

            ApplicationUser user = await UserManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found.";

                return result;
            }
            else
            {
                bool userAccessRecorded = await RecordUserAccess(user, UserAccessPurposes.Details);

                if (userAccessRecorded)
                {
                    UserDetailsViewModel viewModel = user.Adapt<UserDetailsViewModel>();

                    return GenerateArray(viewModel, RootRoute);
                }
                else
                {
                    TempData[TempDataKeys.ErrorMessage] = "Could not access user. Please try again.";

                    return result;
                }
            }
        }

        public async Task<object[]> ManageEdit(string userId)
        {
            object[] result = GenerateArray(null, RootRoute);

            ApplicationUser user = await UserManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found.";

                return result;
            }
            else
            {
                bool userAccessRecorded = await RecordUserAccess(user, UserAccessPurposes.Edit);

                if (userAccessRecorded)
                {
                    EditUserInputModel viewModel = user.Adapt<EditUserInputModel>();

                    return GenerateArray(viewModel, RootRoute);
                }
                else
                {
                    TempData[TempDataKeys.ErrorMessage] = "Could not access user. Please try again.";

                    return result;
                }
            }
        }

        public async Task<string> ManageUpdate(EditUserInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;
            
            ApplicationUser user = await UserManager.FindByIdAsync(inputModel.Id);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found";

                return RootRoute;
            }
            else if (user.FirstName == inputModel.FirstName && user.LastName == inputModel.LastName)
            {
                TempData[TempDataKeys.ErrorMessage] = "Edit fields to update user.";

                return null;
            }
            else
            {
                user.FirstName = inputModel.FirstName;
                user.LastName = inputModel.LastName;

                using IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync();

                bool userAccessRecorded = await RecordUserAccess(user, UserAccessPurposes.Update);

                if (userAccessRecorded)
                {
                    IdentityResult updateUser = await UserManager.UpdateAsync(user);

                    if (!updateUser.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        // updating role failed, adding erros to ModelState
                        foreach (IdentityError error in updateUser.Errors)
                            ActionContext.ModelState.AddModelError(string.Empty, error.Description);

                        return null;
                    }
                    else
                    {
                        await transaction.CommitAsync();

                        TempData[TempDataKeys.SuccessMessage] = "User updated.";

                        return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                    }
                }
                else
                {
                    await transaction.RollbackAsync();

                    TempData[TempDataKeys.ErrorMessage] = "Could not update user. Please try again.";

                    return null;
                }
            }
        }

        private async Task<bool> RecordUserAccess(ApplicationUser user, string purpose)
        {
            ApplicationUser accessor = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            UserAccessRecord userAccessRecord = new UserAccessRecord()
            {
                UserId = user.Id,
                AccessorId = accessor.Id,
                Purpose = purpose
            };

            try
            {
                await DbContext.UserAccessRecords.AddAsync(userAccessRecord);

                await DbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception exception)
            {
                if (exception is DbUpdateException || exception is DbUpdateConcurrencyException)
                {
                    return false;
                }

                throw;
            }
        }

        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}
