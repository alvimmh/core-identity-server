using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class AuthenticationController : Controller
    {
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

        public IActionResult SignIn()
        {
            return View();
        }
    }
}