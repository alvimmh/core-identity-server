using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models;
using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Areas.Enroll.Services;
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
            // result is an array containing the ViewModel & a RouteValueDictionary in consecutive order
            object[] result = await AuthenticationService.ManageEmailChallenge(TempData);

            // if ViewModel is null then redirect to RouteValueDictionary returned from AuthenticationService
            if (result[0] == null)
                return RedirectToRoute(result[1]);

            return View(result[0]);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailChallenge([FromForm] EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await AuthenticationService.ManageEmailChallengeVerification(inputModel);
            if (redirectRouteValues == null)
                return View(inputModel);

            TempData["userEmail"] = inputModel.Email;
            return RedirectToRoute(redirectRouteValues);
        }

        public IActionResult SignIn()
        {
            return View();
        }
    }
}