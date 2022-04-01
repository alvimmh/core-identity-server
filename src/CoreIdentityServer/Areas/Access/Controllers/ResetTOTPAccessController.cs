using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Areas.Access.Services;
using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Models.InputModels;

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
        public IActionResult InitiateTOTPAccessRecoveryChallenge()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiateTOTPAccessRecoveryChallenge([FromForm] InitiateTOTPAccessRecoveryChallengeInputModel inputModel)
        {
            string redirectRoute = await ResetTOTPAccessService.InitiateTOTPAccessRecoveryChallenge(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet]
        public async Task<IActionResult> RecoverTOTPAccessChallenge()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await ResetTOTPAccessService.ManageTOTPAccessRecoveryChallenge();

            // if ViewModel is null then redirect to route returned from ResetTOTPAccessService
            if (result[0] == null)
                return Redirect((string)result[1]);
            
            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RecoverTOTPAccessChallenge([FromForm] TOTPAccessRecoveryChallengeInputModel inputModel)
        {
            string redirectRoute = await ResetTOTPAccessService.ManageTOTPAccessRecoveryChallengeVerification(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }
    }
}