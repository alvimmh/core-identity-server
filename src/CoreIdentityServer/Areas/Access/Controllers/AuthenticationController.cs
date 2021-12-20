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
        private SignUpService SignUpService;

        public AuthenticationController(AuthenticationService authenticationService, SignUpService signUpService)
        {
            AuthenticationService = authenticationService;
            SignUpService = signUpService;
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
            EmailChallengeInputModel model = await AuthenticationService.ManageEmailChallenge(TempData);
            if (model == null)
                return RedirectToRoute(SignUpService.RootRoute());

            return View(model);
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