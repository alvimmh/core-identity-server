using System;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models;
using CoreIdentityServer.Models;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.Email.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private IConfiguration Config;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        private bool ResourcesDisposed;

        public AuthenticationService(
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor
        )
        {
            Config = config;
            UserManager = userManager;
            SignInManager = signInManager;
            EmailService = emailService;
            IdentityService = identityService;
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
                    if (prospectiveUser != null && !prospectiveUser.AccountRegistered && !prospectiveUser.EmailConfirmed)
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
            if (prospectiveUser == null || (!prospectiveUser.AccountRegistered && prospectiveUser.EmailConfirmed))
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

        public RouteValueDictionary ManageSignIn()
        {
            RouteValueDictionary redirectRouteValues = null;

            bool currentUserSignedIn = IdentityService.CheckActiveSession();
            if (currentUserSignedIn)
                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");

            return redirectRouteValues;
        }

        public async Task<RouteValueDictionary> SignIn(SignInInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;
            
            // check if there is a current user logged in, if so redirect to an authorized page
            bool currentUserSignedIn = IdentityService.CheckActiveSession();
            if (currentUserSignedIn)
            {
                redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
                return redirectRouteValues;
            }

            // check if ModelState is valid, if not return ViewModel
            if (!ActionContext.ModelState.IsValid)
                return redirectRouteValues;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            bool userMeetsSignInPrerequisites = await IdentityService.VerifySignInPrerequisites(user);
            if (!userMeetsSignInPrerequisites)
            {
                // add generic error and return ViewModel
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");

                return redirectRouteValues;
            }

            // verify TOTP code
            bool TOTPAccessVerified = await UserManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, inputModel.TOTPCode);
            if (TOTPAccessVerified)
            {
                await IdentityService.ResetSignInAttempts(user);

                // update security stamp of the user so other active sessions are logged out on the next request
                IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(user);
                if (updateSecurityStamp.Succeeded)
                {
                    await SignInManager.SignInAsync(user, false);

                    // send email to user about new session
                    IdentityService.SendConfirmNewActiveSessionEmail("noreply@bonicinitiatives.biz", inputModel.Email, user.UserName);

                    redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
                }
                else
                {
                    Console.WriteLine($"Error updating security stamp");
                    foreach (IdentityError error in updateSecurityStamp.Errors)
                        Console.WriteLine(error.Description);

                    ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");
                }
            }
            else
            {
                await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
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
