using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Enroll.Models;
using CoreIdentityServer.Internals.Constants.TokenProvider;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.EmailService;
using CoreIdentityServer.Models;
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
        public RouteValueDictionary RootRoute;

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
            RootRoute = GenerateRedirectRouteValues("RegisterProspectiveUser", "SignUp", "Enroll");
        }

        public async Task<RouteValueDictionary> RegisterProspectiveUser(ProspectiveUserInputModel userInfo)
        {
            RouteValueDictionary redirectRouteValues = null;

            if (!ActionContext.ModelState.IsValid)
            {
                return redirectRouteValues;
            }

            ApplicationUser existingUser  = await UserManager.FindByEmailAsync(userInfo.Email);
            if (existingUser != null && existingUser.AccountRegistered)
            {
                redirectRouteValues = GenerateRedirectRouteValues("EmailChallengePrompt", "Authentication", "Access");
                return redirectRouteValues;
            }

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

        public async Task<object[]> RegisterTOTPAccess(ITempDataDictionary TempData)
        {
            RegisterTOTPAccessInputModel model = null;
            RouteValueDictionary redirectRouteValues = RootRoute;
            object[] redirectToRootRouteResult = GenerateArray(model, redirectRouteValues);

            bool tempDataExists = TempData.TryGetValue("userEmail", out object tempDataValue);

            string userEmail = tempDataExists ? tempDataValue.ToString() : null;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return redirectToRootRouteResult;
            }

            ApplicationUser prospectiveUser = await UserManager.FindByEmailAsync(userEmail);
            if (prospectiveUser == null)
            {
                return redirectToRootRouteResult;
            }
            else if (prospectiveUser.AccountRegistered)
            {
                redirectRouteValues = GenerateRedirectRouteValues("EmailChallengePrompt", "Authentication", "Access");
                return GenerateArray(model, redirectRouteValues);
            }

            string authenticatorKey = null;
            string authenticatorKeyUri = null;

            IdentityResult resetAuthenticatorKey = await UserManager.ResetAuthenticatorKeyAsync(prospectiveUser);
            if (resetAuthenticatorKey.Succeeded)
            {
                authenticatorKey = await UserManager.GetAuthenticatorKeyAsync(prospectiveUser);
                authenticatorKeyUri = GenerateQRCodeUri(userEmail, authenticatorKey);
            }
            else
            {
                // resetting authenticator key failed, user will be redirected to SignUp root route
                return redirectToRootRouteResult;
            }
            
            // create a session TOTP code valid for 3 mins - when user surpasses 3 mins to scan & submit the TOTP code, request will fail
            string sessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(prospectiveUser, CustomTokenOptions.GenericTOTPTokenProvider);

            model = new RegisterTOTPAccessInputModel()
            {
                AuthenticatorKey = authenticatorKey,
                AuthenticatorKeyUri = authenticatorKeyUri,
                Email = userEmail,
                SessionVerificationTOTPCode = sessionVerificationCode,
            };

            return GenerateArray(model, redirectRouteValues);
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
                redirectRouteValues = RootRoute;
                return redirectRouteValues;
            }
            else if (prospectiveUser.AccountRegistered)
            {
                redirectRouteValues = GenerateRedirectRouteValues("EmailChallengePrompt", "Authentication", "Access");
                return redirectRouteValues;
            }

            bool sessionVerified = await UserManager.VerifyTwoFactorTokenAsync(
                prospectiveUser,
                CustomTokenOptions.GenericTOTPTokenProvider,
                inputModel.SessionVerificationTOTPCode
            );

            if (sessionVerified)
            {
                bool totpAccessVerified = await UserManager.VerifyTwoFactorTokenAsync(
                    prospectiveUser,
                    TokenOptions.DefaultAuthenticatorProvider,
                    inputModel.TOTPCode
                );

                string newSessionVerificationCode = null;

                if (totpAccessVerified)
                {
                    prospectiveUser.TwoFactorEnabled = true;
                    prospectiveUser.AccountRegistered = true;

                    IdentityResult updateUser = await UserManager.UpdateAsync(prospectiveUser);
                    if (updateUser.Succeeded)
                    {
                        redirectRouteValues = GenerateRedirectRouteValues("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");
                    }
                    else
                    {
                        // update user failed but session still valid, generate new session verification code
                        newSessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(prospectiveUser, CustomTokenOptions.GenericTOTPTokenProvider);

                        // Error when enabling Two Factor Authentication, add them to ModelState
                        foreach (IdentityError error in updateUser.Errors)
                            ActionContext.ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    // session still valid, generate new session verification code
                    newSessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(prospectiveUser, CustomTokenOptions.GenericTOTPTokenProvider);

                    // TOTP access verification failed, adding erros to ModelState
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid TOTP code");
                }

                if (newSessionVerificationCode != null)
                    inputModel.SessionVerificationTOTPCode = newSessionVerificationCode;
            }
            else
            {
                // Session verification failed, adding erros to ModelState
                Console.WriteLine("Session verification failed, redirecting to RootRoute");
                redirectRouteValues = RootRoute;
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
