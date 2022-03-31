using System;
using System.Linq;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Users;
using CoreIdentityServer.Internals.Constants.Storage;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace CoreIdentityServer.Areas.Administration.Services
{
    public class UsersService : BaseService, IDisposable
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public UsersService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory
        ) {
            DbContext = dbContext;
            UserManager = userManager;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(actionContextAccessor.ActionContext.HttpContext);
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
                ApplicationUser accessor = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                UserAccessRecord userAccessRecord = new UserAccessRecord()
                {
                    UserId = user.Id,
                    AccessorId = accessor.Id
                };

                try {
                    await DbContext.UserAccessRecords.AddAsync(userAccessRecord);

                    await DbContext.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    if (exception is DbUpdateException || exception is DbUpdateConcurrencyException)
                    {
                        TempData[TempDataKeys.ErrorMessage] = "Could not access user. Please try again.";

                        return result;
                    }

                    throw;
                }

                UserDetailsViewModel viewModel = user.Adapt<UserDetailsViewModel>();

                return GenerateArray(viewModel, RootRoute);
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
