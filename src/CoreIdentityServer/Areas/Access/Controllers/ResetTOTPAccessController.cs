using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Areas.Access.Services;
using Microsoft.AspNetCore.Mvc;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Models.InputModels;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Filters.ActionFilters;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.Access), SecurityHeaders]
    public class ResetTOTPAccessController : Controller
    {
        private ResetTOTPAccessService ResetTOTPAccessService;

        public ResetTOTPAccessController(ResetTOTPAccessService resetTOTPAccessService)
        {
            ResetTOTPAccessService = resetTOTPAccessService;
        }

        public async Task<IActionResult> ManageAuthenticator()
        {
            ManageAuthenticatorViewModel viewModel = await ResetTOTPAccessService.ManageAuthenticator();

            if (viewModel == null)
                return View();

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult InitiateUnauthenticatedRecovery()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, ValidateCaptcha]
        public async Task<IActionResult> InitiateUnauthenticatedRecovery([FromForm] InitiateTOTPAccessRecoveryChallengeInputModel inputModel)
        {
            string redirectRoute = await ResetTOTPAccessService.InitiateTOTPAccessRecoveryChallenge(inputModel);

            return Redirect(redirectRoute);
        }

        [HttpGet, Authorize, Authorize(Policy = Policies.TOTPChallenge)]
        public IActionResult InitiateAuthenticatedRecovery()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> InitiateAuthenticatedRecovery([FromForm] InitiateTOTPAccessRecoveryChallengeInputModel inputModel)
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

        [HttpGet, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> ResetTOTPAccessRecoveryCodes()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await ResetTOTPAccessService.ManageResetTOTPAccessRecoveryCodes();

            // if ViewModel is null then redirect to route returned from ResetTOTPAccessService
            if (result[0] == null)
                return Redirect((string)result[1]);
            
            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> ResetTOTPAccessRecoveryCodes([FromForm] ResetTOTPAccessRecoveryCodesInputModel inputModel)
        {
            string redirectRoute = await ResetTOTPAccessService.ResetTOTPAccessRecoveryCodes(inputModel);

            return Redirect(redirectRoute);
        }
    }
}