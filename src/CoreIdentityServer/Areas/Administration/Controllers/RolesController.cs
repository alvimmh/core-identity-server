using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Roles;
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
    public class RolesController : Controller
    {
        private RolesService RolesService;

        public RolesController(RolesService rolesService)
        {
            RolesService = rolesService;
        }


        /// The HTTP GET action to show the Index page for all roles
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IndexViewModel viewModel = await RolesService.ManageIndex();

            return View(viewModel);
        }


        /// The HTTP GET action to show the role details page
        [HttpGet]
        public async Task<IActionResult> Details([FromRoute] string id)
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await RolesService.ManageDetails(id);

            // if ViewModel is null then redirect to route returned from RolesService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }


        /// The HTTP POST action to update the role details
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Details([FromForm] EditRoleInputModel inputModel)
        {
            string redirectRoute = await RolesService.ManageUpdate(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }


        /// The HTTP GET action to show the create role page
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }


        /// The HTTP POST action to create a role
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] CreateRoleInputModel inputModel)
        {
            string redirectRoute = await RolesService.ManageCreate(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }


        /// The HTTP POST action to delete a role
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] DeleteRoleInputModel inputModel)
        {
            string redirectRoute = await RolesService.ManageDelete(inputModel);

            return Redirect(redirectRoute);
        }
    }
}