using System.Threading.Tasks;
using CoreIdentityServer.Areas.Enroll.Models;
using CoreIdentityServer.Data;
using CoreIdentityServer.Models;
using CoreIdentityServer.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Areas.Enroll.Controllers
{
    [Area("Enroll")]
    public class SignUpController : Controller
    {
        private IConfiguration Config;
        private UserManager<ApplicationUser> UserManager;
        private readonly ApplicationDbContext DbContext;
        private EmailService EmailService;

        public SignUpController(IConfiguration config, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            Config = config;
            UserManager = userManager;
            DbContext = dbContext;
            EmailService = new EmailService(config);
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> RegisterProspectiveUser([FromForm] ProspectiveUserInputModel userInfo)
        {
            ApplicationUser prospectiveUser = new ApplicationUser()
            {
                Email = userInfo.Email,
                UserName = userInfo.Email,
            };

            // create user without password
            IdentityResult identityResult = await UserManager.CreateAsync(prospectiveUser);

            if (identityResult.Succeeded) {
                // generate TOTP based verification code for confirming the user's email
                string verificationCode = await UserManager.GenerateTwoFactorTokenAsync(prospectiveUser, TokenOptions.DefaultEmailProvider);

                string emailSubject = "Please Confirm Your Email";
                string emailBody = $"Greetings, please confirm your email by submitting this verification code: {verificationCode}";

                // user account successfully created, initiate email confirmation
                EmailService.Send("noreply@bonicinitiatives.biz", userInfo.Email, emailSubject, emailBody);

                // cleanup
                Dispose();

                return RedirectToAction("EmailChallenge", "Authentication", new { area = "Access" });
            }

            // registration failed, redirect to SignUp page again
            return Redirect("Index");
        }

        public IActionResult RegisterTOTPAccess()
        {
            return View();
        }

        public IActionResult RegisterTOTPAccessSuccessful()
        {
            return View();
        }

        // dispose of all managed & unmanaged resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (UserManager != null)
                {
                    UserManager.Dispose();
                    UserManager = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
