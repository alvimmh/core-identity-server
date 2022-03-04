using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Enroll.Models.SignUp;
using CoreIdentityServer.Areas.Enroll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routes;

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

        [HttpGet]
        public IActionResult RegisterProspectiveUser()
        {
            string redirectRoute = SignUpService.ManageRegisterProspectiveUser();

            if (redirectRoute != null)
                return Redirect(redirectRoute);

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterProspectiveUser([FromForm] ProspectiveUserInputModel inputModel)
        {
            string redirectRoute = await SignUpService.RegisterProspectiveUser(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await SignUpService.ManageEmailConfirmation();

            // if ViewModel is null then redirect to route returned from SignUpService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail([FromForm] EmailChallengeInputModel inputModel)
        {
            string redirectRoute = await SignUpService.VerifyEmailConfirmation(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet]
        public async Task<IActionResult> RegisterTOTPAccess()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await SignUpService.RegisterTOTPAccess();

            // if ViewModel is null then redirect to url returned from SignUpService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterTOTPAccess([FromForm] RegisterTOTPAccessInputModel inputModel)
        {
            string redirectRoute = await SignUpService.VerifyTOTPAccessRegistration(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet, Authorize]
        public IActionResult RegisterTOTPAccessSuccessful()
        {
            return View();
        }
    }
}
