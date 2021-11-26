using System;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Enroll.Models;
using CoreIdentityServer.Internals.Abstracts;
using CoreIdentityServer.Models;
using CoreIdentityServer.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Areas.Enroll.Services
{
    public class SignUpService : BaseService, IDisposable
    {
        private IConfiguration Config;
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private bool ResourcesDisposed;

        public SignUpService(IConfiguration config, UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            Config = config;
            UserManager = userManager;
            EmailService = emailService;
        }

        public async Task<RouteValueDictionary> RegisterProspectiveUser(ProspectiveUserInputModel userInfo)
        {
            RouteValueDictionary redirectRouteValues;

            ApplicationUser prospectiveUser = new ApplicationUser()
            {
                Email = userInfo.Email,
                UserName = userInfo.Email,
            };

            // create user without password
            IdentityResult identityResult = await UserManager.CreateAsync(prospectiveUser);

            if (identityResult.Succeeded)
            {
                // generate TOTP based verification code for confirming the user's email
                string verificationCode = await UserManager.GenerateTwoFactorTokenAsync(prospectiveUser, TokenOptions.DefaultEmailProvider);

                string emailSubject = "Please Confirm Your Email";
                string emailBody = $"Greetings, please confirm your email by submitting this verification code: {verificationCode}";

                // user account successfully created, initiate email confirmation
                EmailService.Send("noreply@bonicinitiatives.biz", userInfo.Email, emailSubject, emailBody);

                redirectRouteValues = GenerateRedirectRouteValues("EmailChallenge", "Authentication", "Access");
            }
            else
            {
                redirectRouteValues = GenerateRedirectRouteValues("Index", "SignUp", "Enroll");
            }

            // registration failed, redirect to SignUp page again
            return redirectRouteValues;
        }

        // clean up to be done by DI
        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}
