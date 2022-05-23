using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Access.Models.Authentication;
using CoreIdentityServer.Areas.Access.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Filters.ActionFilters;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.Access), SecurityHeaders]
    public class AuthenticationController : Controller
    {
        private AuthenticationService AuthenticationService;

        public AuthenticationController(AuthenticationService authenticationService)
        {
            AuthenticationService = authenticationService;
        }

        [HttpGet]
        public IActionResult AccessDenied([FromQuery] string returnUrl)
        {
            string redirectRoute = AuthenticationService.ManageAccessDenied(returnUrl);

            if (redirectRoute == null)
                return View();
            
            return Redirect(redirectRoute);
        }

        [HttpGet, Authorize]
        public IActionResult TOTPChallenge([FromQuery] string returnUrl)
        {
            TOTPChallengeInputModel viewModel = AuthenticationService.ManageTOTPChallenge(returnUrl);

            return View(viewModel);
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> TOTPChallenge([FromForm] TOTPChallengeInputModel inputModel)
        {
            string redirectRoute = await AuthenticationService.ManageTOTPChallengeVerification(inputModel);

            if (redirectRoute == null)
                return View(inputModel);
            
            return Redirect(redirectRoute);
        }

        [HttpGet, RedirectAuthenticatedUser]
        public async Task<IActionResult> EmailChallenge([FromQuery] string returnUrl)
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await AuthenticationService.ManageEmailChallenge(returnUrl);

            // if ViewModel is null then redirect to route returned from AuthenticationService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser]
        public async Task<IActionResult> EmailChallenge([FromForm] EmailChallengeInputModel inputModel)
        {
            string redirectRoute = await AuthenticationService.ManageEmailChallengeVerification(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet, RedirectAuthenticatedUser]
        public IActionResult SignIn([FromQuery] string returnUrl)
        {
            SignInInputModel viewModel = AuthenticationService.ManageSignIn(returnUrl);

            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser, ValidateCaptcha]
        public async Task<IActionResult> SignIn([FromForm] SignInInputModel inputModel)
        {
            string redirectRoute = await AuthenticationService.SignIn(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet]
        public async Task<IActionResult> SignOut([FromQuery] string signOutId)
        {
            SignOutViewModel viewModel = await AuthenticationService.ManageSignOut(signOutId);

            // check if we need to show sign out prompt
            if (!viewModel.ShowSignOutPrompt)
                return await SignOut(viewModel);

            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOut([FromForm] SignOutInputModel inputModel)
        {
            string redirectRoute = await AuthenticationService.SignOut(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet, RedirectAuthenticatedUser]
        public IActionResult SignedOut()
        {
            SignedOutViewModel viewModel = AuthenticationService.ManageSignedOut();

            return View(viewModel);
        }
    }
}
