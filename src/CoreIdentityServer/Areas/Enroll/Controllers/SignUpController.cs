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

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> RegisterProspectiveUser([FromForm] ProspectiveUserInputModel userInfo)
        {
            RouteValueDictionary redirectionRouteValues = await SignUpService.RegisterProspectiveUser(userInfo);

            return RedirectToRoute(redirectionRouteValues);
        }

        public IActionResult RegisterTOTPAccess()
        {
            return View();
        }

        public IActionResult RegisterTOTPAccessSuccessful()
        {
            return View();
        }
    }
}
