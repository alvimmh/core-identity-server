using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Users;
using CoreIdentityServer.Areas.Administration.Services;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Filters.ResultFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Administration;

namespace CoreIdentityServer.Areas.Administration.Controllers
{
    [
        Area(AreaNames.Administration),
        SecurityHeaders,
        Authorize(Roles = AuthorizedRoles.ProductOwner),
        Authorize(Policy = Policies.TOTPChallenge)
    ]
    public class UsersController : Controller
    {
        private UsersService UsersService;

        public UsersController(UsersService usersService)
        {
            UsersService = usersService;
        }


        /// The HTTP GET action to show all users for the Index page
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string page)
        {
            IndexViewModel viewModel = await UsersService.ManageIndex(page);

            return View(viewModel);
        }


        /// The HTTP POST action to show all users in the search result
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] SearchUsersInputModel inputModel)
        {
            IndexViewModel viewModel = await UsersService.ManageSearch(inputModel);

            return View(viewModel);
        }


        /// The HTTP GET action to show the Details page for a user
        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] string id)
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await UsersService.ManageDetails(id);

            // if ViewModel is null then redirect to route returned from UsersService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }


        /// The HTTP GET action to show the Edit page for a user
        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] string id)
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await UsersService.ManageEdit(id);

            // if ViewModel is null then redirect to route returned from UsersService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }


        /// The HTTP POST action from the Edit page to update a user
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] EditUserInputModel inputModel)
        {
            string redirectRoute = await UsersService.ManageUpdate(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }


        /// The HTTP POST action to block a user
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Block([FromForm] BlockUserInputModel inputModel)
        {
            string redirectRoute = await UsersService.ManageBlock(inputModel, true);

            return Redirect(redirectRoute);
        }


        /// The HTTP POST action to unblock a user
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock([FromForm] BlockUserInputModel inputModel)
        {
            string redirectRoute = await UsersService.ManageBlock(inputModel, false);

            return Redirect(redirectRoute);
        }


        /// The HTTP POST action to delete a user
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] DeleteUserInputModel inputModel)
        {
            string redirectRoute = await UsersService.ManageDelete(inputModel);

            return Redirect(redirectRoute);
        }
    }
}
