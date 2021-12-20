using System;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models;
using CoreIdentityServer.Models;
using CoreIdentityServer.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Abstracts;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private IConfiguration Config;
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private bool ResourcesDisposed;

        public AuthenticationService(IConfiguration config, UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            Config = config;
            UserManager = userManager;
            EmailService = emailService;
        }

        public async Task<EmailChallengeInputModel> ManageEmailChallenge(ITempDataDictionary TempData)
        {
            EmailChallengeInputModel model = null;
            bool tempDataExists = TempData.TryGetValue("userEmail", out object tempDataValue);

            if (tempDataExists)
            {
                string userEmailFromTempData = tempDataValue.ToString();
                if (!string.IsNullOrWhiteSpace(userEmailFromTempData))
                {
                    ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(userEmailFromTempData);
                    if (prospectiveUser != null && !prospectiveUser.EmailConfirmed)
                    {
                        model = new EmailChallengeInputModel
                        {
                            Email = userEmailFromTempData
                        };
                    }
                }
            }

            return model;
        }

        public async Task<RouteValueDictionary> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;

            if (!ValidateModel(inputModel))
            {
                return redirectRouteValues;
            }
            
            ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(inputModel.Email);
            if (prospectiveUser == null)
            {
                redirectRouteValues = GenerateRedirectRouteValues("RegisterProspectiveUser", "SignUp", "Enroll");
                return redirectRouteValues;
            }

            prospectiveUser.EmailConfirmed = await VerifyEmailChallenge(prospectiveUser, inputModel);

            IdentityResult identityResult = prospectiveUser.EmailConfirmed ? await UserManager.UpdateAsync(prospectiveUser) : null;
            if (identityResult != null && identityResult.Succeeded)
            {   
                string emailSubject = "Email Verified";
                string emailBody = $"Congratulations, Your email is now verified.";

                // user account successfully created, initiate email confirmation
                EmailService.Send("noreply@bonicinitiatives.biz", inputModel.Email, emailSubject, emailBody);

                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccess", "SignUp", "Enroll");
            }

            return redirectRouteValues;
        }

        private async Task<bool> VerifyEmailChallenge(ApplicationUser prospectiveUser, EmailChallengeInputModel inputModel)
        {
            // verify email challenge
            bool verificationResult = await UserManager.VerifyTwoFactorTokenAsync(
                prospectiveUser,
                TokenOptions.DefaultEmailProvider,
                inputModel.VerificationCode
            );

            // return verification result
            return verificationResult;
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
