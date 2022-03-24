using System.Threading.Tasks;
using CoreIdentityServer.Areas.ClientServices.Models.Correspondence;
using CoreIdentityServer.Areas.ClientServices.Services;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.ClientServices.Controllers
{
    [Area(AreaNames.ClientServices)]
    public class CorrespondenceController : Controller
    {
        private CorrespondenceService CorrespondenceService;

        public CorrespondenceController(CorrespondenceService correspondenceService)
        {
            CorrespondenceService = correspondenceService;
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmail([FromForm] ResendEmailInputModel inputModel)
        {
            bool resendEmailSucceeded = await CorrespondenceService.ResendEmail(inputModel);

            if (!resendEmailSucceeded)
                return BadRequest(inputModel.ResendEmailErrorMessage);

            return Ok();
        }

        [SecurityHeaders, AllowAnonymous]
        public async Task<IActionResult> Error(string errorType)
        {
            ErrorViewModel viewModel = null;

            if (int.TryParse(errorType, out _))
                viewModel = CorrespondenceService.ManageError(errorType);
            else
                viewModel = await CorrespondenceService.ManageDuendeIdentityServerError(errorType);

            return View(viewModel);
        }
    }
}
