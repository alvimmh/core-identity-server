using System.Threading.Tasks;
using CoreIdentityServer.Areas.Vault.Models.Profile;
using CoreIdentityServer.Areas.Vault.Services;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Routes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CoreIdentityServer.Areas.Vault.Controllers
{
    [Area(AreaNames.Vault), Authorize]
    public class ProfileController : Controller
    {
        private ProfileService ProfileService;
        
        public ProfileController(ProfileService profileService)
        {
            ProfileService = profileService;
        }

        [HttpGet, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> Index()
        {
            // result is an array containing the ViewModel & a RouteValueDictionary in consecutive order
            object[] result = await ProfileService.ManageUserProfile();

            // if ViewModel is null then redirect to route returned from SignUpService
            if (result[0] == null)
                return RedirectToRoute(result[1]);

            return View(result[0]);
        }

        [HttpPost, Authorize(Policy = Policies.TOTPChallenge), ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] UserProfileInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await ProfileService.UpdateUserProfile(inputModel);

            if (redirectRouteValues == null)
                return View(inputModel);

            return RedirectToRoute(redirectRouteValues);
        }
    }
}