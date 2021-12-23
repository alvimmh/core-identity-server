using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Enroll.Models;
using CoreIdentityServer.Internals.Abstracts;
using CoreIdentityServer.Models;
using CoreIdentityServer.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Areas.Enroll.Services
{
    public class SignUpService : BaseService, IDisposable
    {
        private IConfiguration Config;
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private ActionContext ActionContext;
        private bool ResourcesDisposed;
        private readonly UrlEncoder UrlEncoder;

        public SignUpService(
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            UrlEncoder urlEncoder,
            IActionContextAccessor actionContextAccessor
        )
        {
            Config = config;
            UserManager = userManager;
            EmailService = emailService;
            UrlEncoder = urlEncoder;
            ActionContext = actionContextAccessor.ActionContext;
        }

        public RouteValueDictionary RootRoute()
        {
            return GenerateRedirectRouteValues("RegisterProspectiveUser", "SignUp", "Enroll");
        }

        public async Task<RouteValueDictionary> RegisterProspectiveUser(ProspectiveUserInputModel userInfo)
        {
            RouteValueDictionary redirectRouteValues = null;

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRouteValues;
            }

            ApplicationUser existingUser  = await UserManager.FindByEmailAsync(userInfo.Email);

            ApplicationUser prospectiveUser = existingUser ?? new ApplicationUser()
            {
                Email = userInfo.Email,
                UserName = userInfo.Email,
            };
            
            // if user doesn't already exist, create new user without password
            IdentityResult createUser = existingUser == null ? await UserManager.CreateAsync(prospectiveUser) : null;

            // if user already registered with email or user created successfully now, send verification code to confirm email
            if (existingUser != null || createUser.Succeeded)
            {
                // generate TOTP based verification code for confirming the user's email
                string verificationCode = await UserManager.GenerateTwoFactorTokenAsync(prospectiveUser, TokenOptions.DefaultEmailProvider);

                string emailSubject = "Please Confirm Your Email";
                string emailBody = $"Greetings, please confirm your email by submitting this verification code: {verificationCode}";

                // user account successfully created, initiate email confirmation
                EmailService.Send("noreply@bonicinitiatives.biz", userInfo.Email, emailSubject, emailBody);

                redirectRouteValues = GenerateRedirectRouteValues("EmailChallenge", "Authentication", "Access");
            }
            else if (!createUser.Succeeded)
            {
                // new user creation failed, adding erros to ModelState
                foreach (IdentityError error in createUser.Errors)
                    ActionContext.ModelState.AddModelError(string.Empty, error.Description);
            }

            return redirectRouteValues;
        }

        public async Task<RegisterTOTPAccessInputModel> RegisterTOTPAccess(ITempDataDictionary TempData)
        {
            RegisterTOTPAccessInputModel result = null;
            bool tempDataExists = TempData.TryGetValue("userEmail", out object tempDataValue);

            string userEmail = tempDataExists ? tempDataValue.ToString() : null;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return result;
            }

            ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(userEmail);
            if (prospectiveUser == null || prospectiveUser.TwoFactorEnabled)
            {
                return result;
            }

            string authenticatorKey = await UserManager.GetAuthenticatorKeyAsync(prospectiveUser);
            bool authenticatorKeyExists = !string.IsNullOrWhiteSpace(authenticatorKey);
            string authenticatorKeyUri = authenticatorKeyExists ? GenerateQRCodeUri(userEmail, authenticatorKey) : null;

            if (!authenticatorKeyExists)
            {
                IdentityResult resetAuthenticatorKey = await UserManager.ResetAuthenticatorKeyAsync(prospectiveUser);
                if (resetAuthenticatorKey.Succeeded)
                {
                    authenticatorKey = await UserManager.GetAuthenticatorKeyAsync(prospectiveUser);
                    authenticatorKeyUri = GenerateQRCodeUri(userEmail, authenticatorKey);
                }
                else
                {
                    // resetting authenticator key failed, user will be redirected to SignUp root route
                    return result;
                }
            }
            
            result = new RegisterTOTPAccessInputModel()
            {
                AuthenticatorKey = authenticatorKey,
                AuthenticatorKeyUri = authenticatorKeyUri,
                Email = userEmail
            };

            return result;
        }

        public async Task<RouteValueDictionary> VerifyTOTPAccessRegistration(RegisterTOTPAccessInputModel inputModel)
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

            bool totpAccessVerified = await UserManager.VerifyTwoFactorTokenAsync(prospectiveUser, TokenOptions.DefaultAuthenticatorProvider, inputModel.TOTPCode);
            if (totpAccessVerified)
            {
                IdentityResult enableTOTPLogin = await UserManager.SetTwoFactorEnabledAsync(prospectiveUser, true);
                if (enableTOTPLogin.Succeeded)
                {
                    redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
                }
                else
                {
                    // Error when enabling Two Factor Authentication, add them to ModelState
                    foreach (IdentityError error in enableTOTPLogin.Errors)
                        ActionContext.ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                // TOTP access verification failed, adding erros to ModelState
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid TOTP code");
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

        private string GenerateQRCodeUri(string email, string authenticatorKey)
        {
            const string AuthenticatorKeyUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

            return string.Format(
                AuthenticatorKeyUriFormat,
                UrlEncoder.Encode("Bonic"),
                UrlEncoder.Encode(email),
                authenticatorKey
            );
        }
    }
}
