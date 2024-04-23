using CoreIdentityServer.Internals.Constants.Administration;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Filters.ResultFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Administration.Controllers
{
    [Area(AreaNames.Administration), SecurityHeaders, Authorize(Roles = AuthorizedRoles.ProductOwner)]
    public class DashboardController : Controller
    {
        /// The HTTP GET action to show the Dashboard page
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
