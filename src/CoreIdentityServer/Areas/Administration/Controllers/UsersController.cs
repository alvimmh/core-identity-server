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
    }
}
