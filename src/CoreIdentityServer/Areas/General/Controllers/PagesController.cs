using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routing;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Areas.General.Services;
using CoreIdentityServer.Internals.Filters.ResultFilters;

namespace CoreIdentityServer.Areas.General.Controllers
{
    [Area(AreaNames.General), SecurityHeaders, Authorize]
    public class PagesController : Controller
    {
        private PagesService PagesService;

        public PagesController(PagesService pagesService)
        {
            PagesService = pagesService;
        }


        /// The HTTP GET action to show the Dashboard page
        [HttpGet]
        public IActionResult Dashboard([FromQuery] string returnUrl)
        {
            string redirectRoute = PagesService.ManageDashboard(returnUrl);

            if (redirectRoute == null)
                return View();
            
            return Redirect(redirectRoute);
        }
    }
}