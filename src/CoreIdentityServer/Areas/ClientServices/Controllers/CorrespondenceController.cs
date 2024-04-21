using System.Threading.Tasks;
using CoreIdentityServer.Areas.ClientServices.Models.Correspondence;
using CoreIdentityServer.Areas.ClientServices.Services;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Filters.ResultFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.ClientServices.Controllers
{
    [Area(AreaNames.ClientServices), SecurityHeaders]
    public class CorrespondenceController : Controller
    {
        private CorrespondenceService CorrespondenceService;

        public CorrespondenceController(CorrespondenceService correspondenceService)
        {
            CorrespondenceService = correspondenceService;
        }


        /// The HTTP GET action to show the Privacy Policy page
        [HttpGet, AllowAnonymous]
        public IActionResult PrivacyPolicy()
        {
            return View();
        }


        /// The HTTP GET action to show the Terms of Service page
        [HttpGet, AllowAnonymous]
        public IActionResult TermsOfService()
        {
            return View();
        }


        /// The HTTP POST action from the _ResendEmail partial page
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmail([FromForm] ResendEmailInputModel inputModel)
        {
            bool resendEmailSucceeded = await CorrespondenceService.ResendEmail(inputModel);

            if (!resendEmailSucceeded)
                return BadRequest(inputModel.ResendEmailErrorMessage);

            return Ok();
        }


        /// The HTTP GET action to show the Error page. This page is triggered by errors in the application.
        [AllowAnonymous]
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
