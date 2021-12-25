using System;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models;
using CoreIdentityServer.Models;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private IConfiguration Config;
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private ActionContext ActionContext;
        private bool ResourcesDisposed;

        public AuthenticationService(
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor
        )
        {
            Config = config;
            UserManager = userManager;
            EmailService = emailService;
            ActionContext = actionContextAccessor.ActionContext;
        }

        public async Task<object[]> ManageEmailChallenge(ITempDataDictionary TempData)
        {
            EmailChallengeInputModel model = null;
            RouteValueDictionary redirectRouteValues = GenerateRedirectRouteValues("RegisterProspectiveUser", "SignUp", "Enroll");
            bool tempDataExists = TempData.TryGetValue("userEmail", out object tempDataValue);

            if (tempDataExists)
            {
                string userEmailFromTempData = tempDataValue.ToString();
                if (!string.IsNullOrWhiteSpace(userEmailFromTempData))
                {
                    ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(userEmailFromTempData);
                    if (prospectiveUser != null && !prospectiveUser.AccountRegistered)
                    {
                        model = new EmailChallengeInputModel
                        {
                            Email = userEmailFromTempData
                        };
                    }
                    else if (prospectiveUser != null && prospectiveUser.AccountRegistered)
                    {
                        redirectRouteValues = GenerateRedirectRouteValues("EmailChallengePrompt", "Authentication", "Access");
                    }
                }
            }

            return GenerateArray(model, redirectRouteValues);
        }

        public async Task<RouteValueDictionary> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRouteValues;
            }

            ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(inputModel.Email);
            if (prospectiveUser == null)
            {
                redirectRouteValues = GenerateRedirectRouteValues("RegisterProspectiveUser", "SignUp", "Enroll");
                return redirectRouteValues;
            }
            else if (prospectiveUser.AccountRegistered)
            {
                redirectRouteValues = GenerateRedirectRouteValues("EmailChallengePrompt", "Authentication", "Access");
                return redirectRouteValues;
            }

            bool userEmailConfirmed = await VerifyEmailChallenge(prospectiveUser, inputModel);
            if (userEmailConfirmed)
            {
                prospectiveUser.EmailConfirmed = true;

                IdentityResult updateUser = await UserManager.UpdateAsync(prospectiveUser);
                if (updateUser.Succeeded)
                {
                    string emailSubject = "Email Verified";
                    string emailBody = $"Congratulations, Your email is now verified.";

                    // user account successfully created, initiate email confirmation
                    EmailService.Send("noreply@bonicinitiatives.biz", inputModel.Email, emailSubject, emailBody);

                    redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccess", "SignUp", "Enroll");
                }
                else
                {
                    // Error when enabling Two Factor Authentication, add them to ModelState
                    foreach (IdentityError error in updateUser.Errors)
                        ActionContext.ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");

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
