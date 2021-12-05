using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Enroll.Models;
using CoreIdentityServer.Internals.Abstracts;
using CoreIdentityServer.Models;
using CoreIdentityServer.Services.EmailService;
using Microsoft.AspNetCore.Identity;
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
        private bool ResourcesDisposed;
        private readonly UrlEncoder UrlEncoder;

        public SignUpService(IConfiguration config, UserManager<ApplicationUser> userManager, EmailService emailService, UrlEncoder urlEncoder)
        {
            Config = config;
            UserManager = userManager;
            EmailService = emailService;
            UrlEncoder = urlEncoder;
        }

        public RouteValueDictionary RootRoute()
        {
            return GenerateRedirectRouteValues("Index", "SignUp", "Enroll");
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

        public async Task<RegisterTOTPAccessInputModel> RegisterTOTPAccess(ITempDataDictionary TempData)
        {
            bool tempDataExists = TempData.TryGetValue("userEmail", out object tempDataValue);

            string userEmail = tempDataExists ? tempDataValue.ToString() : null;
            if (userEmail == null)
            {
                return null;
            }

            ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(userEmail);
            if (prospectiveUser == null || prospectiveUser.TwoFactorEnabled)
            {
                return null;
            }

            string authenticatorKey = await UserManager.GetAuthenticatorKeyAsync(prospectiveUser);
            string authenticatorKeyUri = authenticatorKey != null ? GenerateQRCodeUri(userEmail, authenticatorKey) : null;
            
            if (authenticatorKey == null)
            {
                IdentityResult resetAuthenticatorKey = await UserManager.ResetAuthenticatorKeyAsync(prospectiveUser);
                if (resetAuthenticatorKey.Succeeded)
                {
                    authenticatorKey = await UserManager.GetAuthenticatorKeyAsync(prospectiveUser);
                    authenticatorKeyUri = GenerateQRCodeUri(userEmail, authenticatorKey);
                }
                else
                {
                    return null;
                }
            }
            
            RegisterTOTPAccessInputModel result = new RegisterTOTPAccessInputModel()
            {
                AuthenticatorKey = authenticatorKey,
                AuthenticatorKeyUri = authenticatorKeyUri,
                Email = userEmail
            };

            return result;
        }

        public async Task<RouteValueDictionary> VerifyTOTPAccessRegistration(RegisterTOTPAccessInputModel inputModel)
        {
            RouteValueDictionary redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccess", "SignUp", "Enroll");
            ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(inputModel.Email);
            string TOTPCode = inputModel.TOTPCode;

            bool totpAccessVerified = await UserManager.VerifyTwoFactorTokenAsync(prospectiveUser, TokenOptions.DefaultAuthenticatorProvider, inputModel.TOTPCode);

            if (totpAccessVerified)
            {
                IdentityResult enableTOTPLogin = await UserManager.SetTwoFactorEnabledAsync(prospectiveUser, true);
                if (enableTOTPLogin.Succeeded)
                {
                    redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
                }
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
