using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Enroll.Models.SignUp;
using CoreIdentityServer.Areas.Enroll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Filters.ResultFilters;

namespace CoreIdentityServer.Areas.Enroll.Controllers
{
    [Area(AreaNames.Enroll), SecurityHeaders]
    public class SignUpController : Controller
    {
        private SignUpService SignUpService;

        public SignUpController(SignUpService signUpService)
        {
            SignUpService = signUpService;
        }


        // The HTTP GET action to show the Register Prospective User page
        [HttpGet, RedirectAuthenticatedUser]
        public IActionResult RegisterProspectiveUser()
        {
            return View();
        }


        // The HTTP POST action from the Register Prospective User page
        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser, ValidateCaptcha]
        public async Task<IActionResult> RegisterProspectiveUser([FromForm] ProspectiveUserInputModel inputModel)
        {
            string redirectRoute = await SignUpService.RegisterProspectiveUser(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }


        // The HTTP GET action to show the Confirm Email page
        [HttpGet, RedirectAuthenticatedUser]
        public async Task<IActionResult> ConfirmEmail()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await SignUpService.ManageEmailConfirmation();

            // if ViewModel is null then redirect to route returned from SignUpService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }


        // The HTTP POST action from the Confirm Email page
        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser]
        public async Task<IActionResult> ConfirmEmail([FromForm] EmailChallengeInputModel inputModel)
        {
            string redirectRoute = await SignUpService.VerifyEmailConfirmation(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }


        // The HTTP GET action to show the Register TOTP Access page
        [HttpGet, RedirectAuthenticatedUser]
        public async Task<IActionResult> RegisterTOTPAccess()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await SignUpService.RegisterTOTPAccess();

            // if ViewModel is null then redirect to url returned from SignUpService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }


        // The HTTP POST action from the Register TOTP Access page
        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser]
        public async Task<IActionResult> RegisterTOTPAccess([FromForm] RegisterTOTPAccessInputModel inputModel)
        {
            string redirectRoute = await SignUpService.VerifyTOTPAccessRegistration(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }


        // The HTTP GET action to show the Register TOTP Access Successful page
        [HttpGet, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> RegisterTOTPAccessSuccessful([FromQuery] bool resetAccess)
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await SignUpService.ManageTOTPAccessSuccessfulRegistration(resetAccess);

            // if ViewModel is null then redirect to url returned from SignUpService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }
    }
}
