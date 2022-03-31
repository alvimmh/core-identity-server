using System;
using System.Linq;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Users;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreIdentityServer.Areas.Administration.Services
{
    public class UsersService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public UsersService(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
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

        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}
