using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Internals.Filters.ResultFilters;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Areas.Access.Models.MFA;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.Access), SecurityHeaders, Authorize]
    public class MFAController : Controller
    {
        private MFAService MFAService;

        public MFAController(MFAService mfaService)
        {
            MFAService = mfaService;
        }


        /// The HTTP GET action to show the Manage Email Authentication page
        [HttpGet, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> ManageEmailAuthentication()
        {
            // result is an array containing the ViewModel & a redirect url in consecutive order
            object[] result = await MFAService.ManageEmailAuthentication();

            // if ViewModel is null then redirect to route returned from MFAService
            if (result[0] == null)
                return Redirect((string)result[1]);
            
            return View(result[0]);
        }


        /// The HTTP POST action from the Manage Email Authentication page
        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> ManageEmailAuthentication([FromForm] SetEmailAuthenticationInputModel inputModel)
        {
            string redirectRoute = await MFAService.SetEmailAuthentication(inputModel);

            return Redirect(redirectRoute);
        }
    }
}
