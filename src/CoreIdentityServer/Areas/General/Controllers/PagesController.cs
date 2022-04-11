using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routes;
using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.General), Authorize]
    public class PagesController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}