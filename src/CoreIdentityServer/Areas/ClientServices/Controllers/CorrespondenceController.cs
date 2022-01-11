using System.Threading.Tasks;
using CoreIdentityServer.Areas.ClientServices.Models.Correspondence;
using CoreIdentityServer.Areas.ClientServices.Services;
using CoreIdentityServer.Internals.Constants.Routes;
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
    }
}