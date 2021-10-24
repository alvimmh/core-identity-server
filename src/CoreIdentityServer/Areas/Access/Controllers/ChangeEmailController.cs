using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class ChangeEmailController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}