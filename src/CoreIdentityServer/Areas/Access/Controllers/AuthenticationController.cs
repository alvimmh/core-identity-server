using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class AuthenticationController : Controller
    {
        public IActionResult RegisterTOTPAccess()
        {
            return View();
        }

        public IActionResult RegisterTOTPAccessSuccessful()
        {
            return View();
        }

        public IActionResult TOTPChallenge()
        {
            return View();
        }

        public IActionResult EmailChallengePrompt()
        {
            return View();
        }

        public IActionResult EmailChallenge()
        {
            return View();
        }
    }
}