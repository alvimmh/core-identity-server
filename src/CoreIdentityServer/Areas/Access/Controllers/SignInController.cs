using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class SignInController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}