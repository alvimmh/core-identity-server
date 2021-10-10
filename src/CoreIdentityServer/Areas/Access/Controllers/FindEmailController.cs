using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class FindEmailController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult FindEmailSuccessful()
        {
            return View();
        }

        public IActionResult FindEmailTips()
        {
            return View();
        }
    }
}