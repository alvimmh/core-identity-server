using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Enroll.Controllers
{
    [Area("Enroll")]
    public class SignUpController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RegisterTOTPAccess()
        {
            return View();
        }

        public IActionResult RegisterTOTPAccessSuccessful()
        {
            return View();
        }
    }
}