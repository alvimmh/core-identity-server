using System.Threading.Tasks;
using CoreIdentityServer.Areas.Vault.Models.Profile;
using CoreIdentityServer.Areas.Vault.Services;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Filters.ResultFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Areas.Vault.Controllers
{
    [Area(AreaNames.Vault), SecurityHeaders]
    public class ProfileController : Controller
    {
        private ProfileService ProfileService;
        
        public ProfileController(ProfileService profileService)
        {
            ProfileService = profileService;
        }


        /// The HTTP GET action to show the Index page
        [HttpGet, Authorize, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> Index()
        {
            // result is an array containing the ViewModel & a redirect to url in consecutive order
            object[] result = await ProfileService.ManageUserProfile();

            // if ViewModel is null then redirect to route returned from SignUpService
            if (result[0] == null)
                return Redirect((string)result[1]);

            return View(result[0]);
        }


        /// The HTTP POST action from the Index page
        [HttpPost, Authorize, Authorize(Policy = Policies.TOTPChallenge), ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] UserProfileInputModel inputModel)
        {
            string redirectRoute = await ProfileService.UpdateUserProfile(inputModel);

            if (redirectRoute == null)
                return View(inputModel);

            return Redirect(redirectRoute);
        }


        /// The HTTP POST action from one of the identity server clients to get a user's email address
        [HttpPost, Authorize(Policy = Policies.AdministrativeAccess), Authorize(Policy = Policies.ClientCredentials)]
        public async Task<IActionResult> UserEmail([FromForm] UserEmailInputModel inputModel)
        {
            string userEmail = await ProfileService.GetUserEmail(inputModel);

            if (userEmail == null)
                return NotFound();

            return Ok(new { email = userEmail });
        }
    }
}