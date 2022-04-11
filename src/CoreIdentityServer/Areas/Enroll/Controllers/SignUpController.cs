using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Enroll.Models.SignUp;
using CoreIdentityServer.Areas.Enroll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using CoreIdentityServer.Internals.Constants.Authorization;

namespace CoreIdentityServer.Areas.Enroll.Controllers
{
    [Area(AreaNames.Enroll)]
    public class SignUpController : Controller
    {
        private SignUpService SignUpService;

        public SignUpController(SignUpService signUpService)
        {
            SignUpService = signUpService;
        }

        [HttpGet, RedirectAuthenticatedUser]
        public IActionResult RegisterProspectiveUser()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser]
        public async Task<IActionResult> RegisterProspectiveUser([FromForm] ProspectiveUserInputModel inputModel)
        {
            string redirectRoute = await SignUpService.RegisterProspectiveUser(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

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

        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser]
        public async Task<IActionResult> ConfirmEmail([FromForm] EmailChallengeInputModel inputModel)
        {
            string redirectRoute = await SignUpService.VerifyEmailConfirmation(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

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

        [HttpPost, ValidateAntiForgeryToken, RedirectAuthenticatedUser]
        public async Task<IActionResult> RegisterTOTPAccess([FromForm] RegisterTOTPAccessInputModel inputModel)
        {
            string redirectRoute = await SignUpService.VerifyTOTPAccessRegistration(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

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
