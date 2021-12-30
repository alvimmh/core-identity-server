using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Services.Email.EmailService;
using CoreIdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CoreIdentityServer.Internals.Services.Identity.IdentityService
{
    public class IdentityService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private EmailService EmailService;
        private ActionContext ActionContext;
        private bool ResourcesDisposed;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor
        ) {
            UserManager = userManager;
            SignInManager = signInManager;
            EmailService = emailService;
            ActionContext = actionContextAccessor.ActionContext;
        }

        public async Task RecordUnsuccessfulSignInAttempt(ApplicationUser user)
        {
            IdentityResult saveUnsuccessfulAttempt = await UserManager.AccessFailedAsync(user);
            if (!saveUnsuccessfulAttempt.Succeeded)
            {
                Console.WriteLine($"Could not record unsuccessful SignIn attempt.");

                foreach (IdentityError error in saveUnsuccessfulAttempt.Errors)
                    Console.WriteLine(error.Description);
            }

            // if increasing the failed count results in account lockout, notify the user
            bool isUserLockedOut = await UserManager.IsLockedOutAsync(user);
            if (isUserLockedOut)
                SendAccountLockedOutEmail("noreply@bonicinitiatives.biz", user.Email, user.UserName);
        }

        public async Task ResetSignInAttempts(ApplicationUser user)
        {
            IdentityResult resetAttempts = await UserManager.ResetAccessFailedCountAsync(user);
            if (!resetAttempts.Succeeded)
            {
                Console.WriteLine($"Cound not record successful SignIn attempt.");

                foreach (IdentityError error in resetAttempts.Errors)
                    Console.WriteLine(error.Description);
            }
        }

        public async Task<bool> VerifySignInPrerequisites(ApplicationUser user)
        {
            // check if user doesn't exist with the given email
            if (user == null)
                return false;

            // if user exists but did not complete registration, send email to complete registration
            if (!user.AccountRegistered)
            {
                SendAccountNotRegisteredEmail("noreply@bonicinitiatives.biz", user.Email, user.UserName);

                return false;
            }

            // check if user is allowed to sign in && user is not locked out            
            bool canUserSignIn = await SignInManager.CanSignInAsync(user);
            bool appSupportsLockout = UserManager.SupportsUserLockout;
            bool isUserLockedOut = await UserManager.IsLockedOutAsync(user);

            if (!canUserSignIn || (appSupportsLockout && isUserLockedOut))
                return false;

            return true;
        }

        // check if there is a current user logged in, if so redirect to an authorized page
        public bool CheckActiveSession()
        {
            return SignInManager.IsSignedIn(ActionContext.HttpContext.User);
        }

        // notify the user about account lockout
        public void SendAccountLockedOutEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "Account Locked Out";
            string emailBody = $"Dear {userName}, due to 3 unsuccessful attempts to sign in to your account, we have locked it out. You can try again in 30 minutes or click this link to reset your TOTP access.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // notify user about new session
        public void SendConfirmNewActiveSessionEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "New Active Session Started";
            string emailBody = $"Dear {userName}, this is to notify you of a new active session.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        // notify user to complete account registration
        public void SendAccountNotRegisteredEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "SignIn Attempt Detected";
            string emailBody = $"Dear {userName}, we have detected a sign in attempt for your account. To log in, you need to finish registration.";

            EmailService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}
