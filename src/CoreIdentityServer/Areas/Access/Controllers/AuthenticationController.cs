using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models;
using CoreIdentityServer.Areas.Access.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area("Access")]
    public class AuthenticationController : Controller
    {
        private AuthenticationService AuthenticationService;

        public AuthenticationController(AuthenticationService authenticationService)
        {
            AuthenticationService = authenticationService;
        }

        public IActionResult TOTPChallenge()
        {
            return View();
        }

        public IActionResult EmailChallengePrompt()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EmailChallenge()
        {
            RouteValueDictionary redirectRouteValues = await AuthenticationService.ManageEmailChallenge(TempData);

            if (redirectRouteValues != null)
                return RedirectToRoute(redirectRouteValues);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailChallengeVerification([FromForm] EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await AuthenticationService.ManageEmailChallengeVerification(inputModel);

            TempData["userEmail"] = inputModel.Email;
            return RedirectToRoute(redirectRouteValues);
        }

        public IActionResult SignIn()
        {
            return View();
        }
    }
}