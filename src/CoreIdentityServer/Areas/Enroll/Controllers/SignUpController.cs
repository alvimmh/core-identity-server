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
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterProspectiveUser([FromForm] ProspectiveUserInputModel userInfo)
        {
            RouteValueDictionary redirectRouteValues = await SignUpService.RegisterProspectiveUser(userInfo);

            TempData["userEmail"] = userInfo.Email;
            return RedirectToRoute(redirectRouteValues);
        }

        [HttpGet]
        public async Task<IActionResult> RegisterTOTPAccess()
        {
            RegisterTOTPAccessInputModel model = await SignUpService.RegisterTOTPAccess(TempData);
            if (model == null)
                return RedirectToRoute(SignUpService.RootRoute());

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTOTPAccessRegistration([FromForm] RegisterTOTPAccessInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = await SignUpService.VerifyTOTPAccessRegistration(inputModel);

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
