using System.Threading.Tasks;
using CoreIdentityServer.Areas.Enroll.Models;
using CoreIdentityServer.Areas.Enroll.Services;
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
        public async Task<IActionResult> RegisterTOTPAccess()
        {
            // result is an array containing the ViewModel & a RouteValueDictionary in consecutive order
            object[] result = await SignUpService.RegisterTOTPAccess(TempData);

            // if ViewModel is null then redirect to RouteValueDictionary returned from SignUpService
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

        [HttpGet]
        public IActionResult RegisterTOTPAccessSuccessful()
        {
            return View();
        }
    }
}
