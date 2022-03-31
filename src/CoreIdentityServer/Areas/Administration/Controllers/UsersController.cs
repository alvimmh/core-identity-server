using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Users;
using CoreIdentityServer.Areas.Administration.Services;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Administration.Controllers
{
    [Area(AreaNames.Administration), SecurityHeaders, Authorize(Roles = AuthorizedRoles.ProductOwner), Authorize(Policy = Policies.TOTPChallenge)]
    public class UsersController : Controller
    {
        private UsersService UsersService;

        public UsersController(UsersService usersService)
        {
            UsersService = usersService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string page)
        {
            IndexViewModel viewModel = await UsersService.ManageIndex(page);

            return View(viewModel);
        }

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

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] EditUserInputModel inputModel)
        {
            string redirectRoute = await UsersService.ManageUpdate(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }
    }
}
