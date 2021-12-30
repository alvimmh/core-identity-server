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
using CoreIdentityServer.Internals.Constants.TokenProvider;

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
        public RouteValueDictionary RootRoute;
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
            RootRoute = GenerateRedirectRouteValues("SignIn", "Authentication", "Access");
        }

        public async Task<object[]> ManageEmailChallenge(ITempDataDictionary tempData)
        {
            EmailChallengeInputModel model = null;
            RouteValueDictionary redirectRouteValues = RootRoute;
            object[] result = GenerateArray(model, redirectRouteValues);

            bool tempDataExists = tempData.TryGetValue("userEmail", out object tempDataValue);
            if (tempDataExists)
            {
                string userEmailFromTempData = tempDataValue.ToString();
                if (!string.IsNullOrWhiteSpace(userEmailFromTempData))
                {
                    ApplicationUser user = await UserManager.FindByEmailAsync(userEmailFromTempData);

                    if (user == null || !user.EmailConfirmed)
                    {
                        // user doesn't exist, redirect to sign in page
                        return result;
                    }
                    else if (user.EmailConfirmed && !user.AccountRegistered)
                    {
                        // user exists, send email to complete registration
                        IdentityService.SendAccountNotRegisteredEmail("noreply@bonicinitiatives.biz", userEmailFromTempData, user.UserName);
                        return result;
                    }
                    else if (user.EmailConfirmed && user.AccountRegistered)
                    {
                        // user exists and completed registration
                        model = new EmailChallengeInputModel
                        {
                            Email = userEmailFromTempData
                        };
                    }
                }
            }

            return GenerateArray(model, redirectRouteValues);
        }

        public async Task<RouteValueDictionary> VerifyEmailChallenge(EmailChallengeInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = null;

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRouteValues;
            }

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);
            if (user == null || !user.EmailConfirmed)
            {
                // user doesn't exist, redirect to sign in page
                redirectRouteValues = RootRoute;
                return redirectRouteValues;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists, send email to complete registration & redirect to sign in page
                IdentityService.SendAccountNotRegisteredEmail("noreply@bonicinitiatives.biz", inputModel.Email, user.UserName);
                redirectRouteValues = RootRoute;

                return redirectRouteValues;
            }
            else if (user.EmailConfirmed && user.AccountRegistered)
            {
                // if TOTP code verified, sign in the user
                bool TOTPCodeVerified = await IdentityService.VerifyTOTPCode(user, CustomTokenOptions.GenericTOTPTokenProvider, inputModel.VerificationCode);
                if (TOTPCodeVerified)
                {
                    // update security stamp of the user so other active sessions are logged out on the next request
                    IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(user);
                    if (updateSecurityStamp.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, false);

                        // send email to user about new session
                        IdentityService.SendNewActiveSessionNotificationEmail("noreply@bonicinitiatives.biz", inputModel.Email, user.UserName);

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
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
            }           

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

                string sessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

                IdentityService.SendNewSessionVerificationEmail("noreply@bonicinitiatives.biz", inputModel.Email, user.UserName, sessionVerificationCode);

                redirectRouteValues = GenerateRedirectRouteValues("EmailChallenge", "Authentication", "Access");
            }
            else
            {
                await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
            }

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
