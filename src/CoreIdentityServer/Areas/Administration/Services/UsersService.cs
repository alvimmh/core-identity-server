using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Users;
using CoreIdentityServer.Internals.Constants.Administration;
using CoreIdentityServer.Internals.Constants.Storage;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
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
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        private IUrlHelper UrlHelper;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public UsersService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IUrlHelperFactory urlHelperFactory
        ) {
            DbContext = dbContext;
            UserManager = userManager;
            IdentityService = identityService;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(actionContextAccessor.ActionContext.HttpContext);
            UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            RootRoute = GenerateRouteUrl("Index", "Users", "Administration");
        }

        public async Task<IndexViewModel> ManageIndex(string page)
        {
            IndexViewModel viewModel = new IndexViewModel();

            int totalUsers = await UserManager.Users.CountAsync();
            viewModel.TotalResults = totalUsers;
            
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

        public async Task<IndexViewModel> ManageSearch(SearchUsersInputModel inputModel)
        {
            List<UserViewModel> searchResults = new List<UserViewModel>();

            bool idEmpty = string.IsNullOrWhiteSpace(inputModel.Id);
            bool emailEmpty = string.IsNullOrWhiteSpace(inputModel.Email);
            bool firstNameEmpty = string.IsNullOrWhiteSpace(inputModel.FirstName);
            bool lastNameEmpty = string.IsNullOrWhiteSpace(inputModel.LastName);

            if (idEmpty && emailEmpty && firstNameEmpty && lastNameEmpty)
            {
                ActionContext.ModelState.AddModelError(string.Empty, "Please specify a search parameter.");

                return new IndexViewModel { Users = searchResults };
            }

            if (!idEmpty)
            {
                ApplicationUser user = await UserManager.FindByIdAsync(inputModel.Id);

                if (user != null)
                    searchResults.Add(user.Adapt<UserViewModel>());

                return new IndexViewModel { Id = inputModel.Id, Users = searchResults, TotalResults = searchResults.Count };
            }

            if (!emailEmpty)
            {
                ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

                if (user != null)
                    searchResults.Add(user.Adapt<UserViewModel>());

                return new IndexViewModel { Email = inputModel.Email, Users = searchResults, TotalResults = searchResults.Count };
            }

            if (!firstNameEmpty || !lastNameEmpty)
            {
                IndexViewModel viewModel = new IndexViewModel() {
                    FirstName = inputModel.FirstName,
                    LastName = inputModel.LastName
                };

                Expression<Func<ApplicationUser, bool>> searchFilter = GenerateNamesFilter(inputModel.FirstName, inputModel.LastName, firstNameEmpty, lastNameEmpty);

                int totalMatchedUsers = await UserManager.Users.Where(searchFilter).CountAsync();

                double totalPages = (double)totalMatchedUsers / viewModel.ResultsInPage;
                viewModel.TotalPages = (int)Math.Ceiling(totalPages);
                viewModel.TotalResults = totalMatchedUsers;

                int skipRecords = 0;

                bool isPageNumberInputValid = Int32.TryParse(inputModel.Page, out int currentPage);

                if (isPageNumberInputValid && currentPage > 0 && currentPage <= viewModel.TotalPages)
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

                searchResults = await UserManager.Users
                                                    .Where(searchFilter)
                                                    .OrderBy(item => item.CreatedAt)
                                                    .Skip(skipRecords)
                                                    .Take(viewModel.ResultsInPage)
                                                    .ProjectToType<UserViewModel>()
                                                    .ToListAsync();

                viewModel.Users = searchResults;

                return viewModel;
            }

            return new IndexViewModel() { Users = searchResults };
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

                        // updating user failed, adding erros to ModelState
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

        public async Task<string> ManageBlock(BlockUserInputModel inputModel, bool blockUser)
        {
            if (!ActionContext.ModelState.IsValid)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found.";

                return RootRoute;
            }

            ApplicationUser user = await UserManager.FindByIdAsync(inputModel.Id);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found";

                return RootRoute;
            }
            else
            {
                string blockAction = blockUser ? UserAccessPurposes.Block : UserAccessPurposes.Unblock;
                string blockActionLowerCase = blockAction.ToLower();

                user.ToggleBlock(blockUser);

                using IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync();

                bool userAccessRecorded = await RecordUserAccess(user, blockAction);


                if (userAccessRecorded)
                {
                    IdentityResult updateBlockedStatus = null;
                    
                    if (blockUser)
                    {
                        // update blocked status and the security stamp of user in CIS
                        updateBlockedStatus = await UserManager.UpdateSecurityStampAsync(user);
                    }
                    else
                    {
                        // update blocked status
                        updateBlockedStatus = await UserManager.UpdateAsync(user);
                    }

                    if (!updateBlockedStatus.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        // updating user failed, adding errors to ModelState
                        foreach (IdentityError error in updateBlockedStatus.Errors)
                            Console.WriteLine(error.Description);

                        TempData[TempDataKeys.ErrorMessage] = $"Could not {blockActionLowerCase} user. Please try again.";

                        return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                    }
                    else
                    {
                        await transaction.CommitAsync();

                        if (blockUser)
                        {
                            // send notifications to all clients of CIS to signout the user
                            await IdentityService.SendBackChannelLogoutNotificationsForUserAsync(user);
                        }

                        TempData[TempDataKeys.SuccessMessage] = $"User {blockActionLowerCase}ed.";

                        return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                    }
                }
                else
                {
                    await transaction.RollbackAsync();

                    TempData[TempDataKeys.ErrorMessage] = $"Could not {blockActionLowerCase} user. Please try again.";

                    return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
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

        private Expression<Func<ApplicationUser, bool>> GenerateNamesFilter(string firstName, string lastName, bool firstNameEmpty, bool lastNameEmpty)
        {
            Expression<Func<ApplicationUser, bool>> filter = null;

            string firstNameLowerCase = firstName?.ToLower();
            string lastNameLowerCase = lastName?.ToLower();

            if (!firstNameEmpty && !lastNameEmpty)
            {
                filter = user => user.FirstName.ToLower().Contains(firstNameLowerCase) && user.LastName.ToLower().Contains(lastNameLowerCase);
            }
            else if (!firstNameEmpty)
            {
                filter = user => user.FirstName.ToLower().Contains(firstNameLowerCase);
            }
            else if (!lastNameEmpty)
            {
                filter = user => user.LastName.ToLower().Contains(lastNameLowerCase);
            }

            return filter;
        }

        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            DbContext.Dispose();
            ResourcesDisposed = true;
        }
    }
}
