using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Areas.Access.Services;
using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routes;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.Access)]
    public class ResetTOTPAccessController : Controller
    {
        private ResetTOTPAccessService ResetTOTPAccessService;
        public ResetTOTPAccessController(ResetTOTPAccessService resetTOTPAccessService)
        {
            ResetTOTPAccessService = resetTOTPAccessService;
        }

        public IActionResult Prompt()
        {
            return View();
        }

        [HttpGet]
        public IActionResult InitiateEmailChallenge()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateEmailChallenge([FromForm] InitiateEmailChallengeInputModel inputModel)
        {
            string redirectRoute = await ResetTOTPAccessService.InitiateEmailChallenge(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet]
        public async Task<IActionResult> EmailChallenge()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await ResetTOTPAccessService.ManageEmailChallenge();

            // if ViewModel is null then redirect to route returned from ResetTOTPAccessService
            if (result[0] == null)
                return Redirect((string)result[1]);
            
            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailChallenge([FromForm] EmailChallengeInputModel inputModel)
        {
            string redirectRoute = await ResetTOTPAccessService.ManageEmailChallengeVerification(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }
    }
}