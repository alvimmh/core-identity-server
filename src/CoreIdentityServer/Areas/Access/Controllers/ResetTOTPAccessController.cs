using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class ResetTOTPAccessController : Controller
    {
        public IActionResult ResetTOTPAccessSuccessful()
        {
            return View();
        }
    }
}