using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Areas.Access.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Constants.Storage;

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
            RouteValueDictionary redirectRouteValues = await ResetTOTPAccessService.InitiateEmailChallenge(inputModel);

            TempData[TempDataKeys.UserEmail] = inputModel.Email;
            bool resendEmailRecordIdExists = ControllerContext.HttpContext.Items.TryGetValue(
                HttpContextItemKeys.ResendEmailRecordId,
                out object resendEmailRecordIdValue
            );

            if (resendEmailRecordIdExists)
                TempData[TempDataKeys.ResendEmailRecordId] = resendEmailRecordIdValue.ToString();

            return RedirectToRoute(redirectRouteValues);
        }

        [HttpGet]
        public async Task<IActionResult> EmailChallenge()
        {
            // result is an array containing the ViewModel & a RouteValueDictionary in consecutive order
            object[] result = await ResetTOTPAccessService.ManageEmailChallenge(TempData);

            // if ViewModel is null then redirect to route returned from ResetTOTPAccessService
            if (result[0] == null)
                return RedirectToRoute(result[1]);
            
            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailChallenge([FromForm] EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await ResetTOTPAccessService.ManageEmailChallengeVerification(inputModel);

            if (redirectRouteValues == null)
                return View(inputModel);

            TempData[TempDataKeys.UserEmail] = inputModel.Email;

            return RedirectToRoute(redirectRouteValues);
        }
    }
}