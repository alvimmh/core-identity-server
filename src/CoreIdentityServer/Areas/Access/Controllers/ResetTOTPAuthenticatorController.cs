using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class ResetTOTPAuthenticatorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ResetTOTPAuthenticatorSuccessful()
        {
            return View();
        }
    }
}