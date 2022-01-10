using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Access.Models.Authentication;
using CoreIdentityServer.Areas.Access.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Internals.Constants.Routes;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.Access)]
    public class AuthenticationController : Controller
    {
        private AuthenticationService AuthenticationService;

        public AuthenticationController(AuthenticationService authenticationService)
        {
            AuthenticationService = authenticationService;
        }

        [HttpGet, Authorize]
        public IActionResult TOTPChallenge()
        {
            return View();
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> TOTPChallenge([FromForm] TOTPChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await AuthenticationService.ManageTOTPChallengeVerification(inputModel);

            if (redirectRouteValues == null)
                return View(inputModel);
            
            return RedirectToRoute(redirectRouteValues);
        }

        [HttpGet]
        public async Task<IActionResult> EmailChallenge()
        {
            // result is an array containing the ViewModel & a RouteValueDictionary in consecutive order
            object[] result = await AuthenticationService.ManageEmailChallenge(TempData);

            // if ViewModel is null then redirect to route returned from AuthenticationService
            if (result[0] == null)
                return RedirectToRoute(result[1]);

            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailChallenge([FromForm] EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await AuthenticationService.ManageEmailChallengeVerification(inputModel);

            if (redirectRouteValues == null)
                return View(inputModel);

            return RedirectToRoute(redirectRouteValues);
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            RouteValueDictionary redirectRouteValues = AuthenticationService.ManageSignIn();

            if (redirectRouteValues != null)
                return RedirectToRoute(redirectRouteValues);

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn([FromForm] SignInInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await AuthenticationService.SignIn(inputModel);

            if (redirectRouteValues == null)
                return View(inputModel);

            TempData["userEmail"] = inputModel.Email;

            return RedirectToRoute(redirectRouteValues);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOut()
        {
            RouteValueDictionary redirectRouteValues = await AuthenticationService.SignOut();

            return RedirectToRoute(redirectRouteValues);
        }
    }
}