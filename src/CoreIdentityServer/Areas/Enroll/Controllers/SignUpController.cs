using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Enroll.Models.SignUp;
using CoreIdentityServer.Areas.Enroll.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CoreIdentityServer.Areas.Enroll.Controllers
{
    [Area("Enroll")]
    public class SignUpController : Controller
    {
        private SignUpService SignUpService;

        public SignUpController(SignUpService signUpService)
        {
            SignUpService = signUpService;
        }

        [HttpGet]
        public IActionResult RegisterProspectiveUser()
        {
            RouteValueDictionary redirectRouteValues = SignUpService.ManageRegisterProspectiveUser();

            if (redirectRouteValues != null)
                return RedirectToRoute(redirectRouteValues);

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterProspectiveUser([FromForm] ProspectiveUserInputModel userInfo)
        {
            RouteValueDictionary redirectRouteValues = await SignUpService.RegisterProspectiveUser(userInfo);

            if (redirectRouteValues == null)
                return View(userInfo);

            TempData["userEmail"] = userInfo.Email;

            return RedirectToRoute(redirectRouteValues);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail()
        {
            // result is an array containing the ViewModel & a RouteValueDictionary in consecutive order
            object[] result = await SignUpService.ManageEmailConfirmation(TempData);

            // if ViewModel is null then redirect to route returned from SignUpService
            if (result[0] == null)
                return RedirectToRoute(result[1]);

            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail([FromForm] EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await SignUpService.VerifyEmailConfirmation(inputModel);

            if (redirectRouteValues == null)
                return View(inputModel);

            TempData["userEmail"] = inputModel.Email;

            return RedirectToRoute(redirectRouteValues);
        }

        [HttpGet]
        public async Task<IActionResult> RegisterTOTPAccess()
        {
            // result is an array containing the ViewModel & a RouteValueDictionary in consecutive order
            object[] result = await SignUpService.RegisterTOTPAccess(TempData);

            // if ViewModel is null then redirect to route returned from SignUpService
            if (result[0] == null)
                return RedirectToRoute(result[1]);

            return View(result[0]);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterTOTPAccess([FromForm] RegisterTOTPAccessInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await SignUpService.VerifyTOTPAccessRegistration(inputModel);

            if (redirectRouteValues == null)
                return View(inputModel);

            TempData["userEmail"] = inputModel.Email;

            return RedirectToRoute(redirectRouteValues);
        }

        [HttpGet, Authorize]
        public IActionResult RegisterTOTPAccessSuccessful()
        {
            return View();
        }
    }
}
