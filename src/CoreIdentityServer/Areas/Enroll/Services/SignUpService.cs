using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Enroll.Models.SignUp;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Constants.UserActions;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Storage;

namespace CoreIdentityServer.Areas.Enroll.Services
{
    public class SignUpService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public SignUpService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService,
            IdentityService identityService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory
        ) {
            UserManager = userManager;
            SignInManager = signInManager;
            EmailService = emailService;
            IdentityService = identityService;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            RootRoute = GenerateRouteUrl("RegisterProspectiveUser", "SignUp", "Enroll");
        }

        public async Task<string> RegisterProspectiveUser(ProspectiveUserInputModel inputModel)
        {
            string redirectRoute = null;

            if (!ActionContext.ModelState.IsValid)
                return redirectRoute;

            ApplicationUser existingUser  = await UserManager.FindByEmailAsync(inputModel.Email);

            if (existingUser != null && existingUser.AccountRegistered)
            {
                redirectRoute = GenerateRouteUrl("Prompt", "ResetTOTPAccess", "Access");

                return redirectRoute;
            }
            else if (existingUser != null && existingUser.EmailConfirmed)
            {
                // since user didn't complete registration process, user needs to go through email confirmation again
                existingUser.EmailConfirmed = false;

                IdentityResult updateUserEmailConfirmationStatus = await UserManager.UpdateAsync(existingUser);

                if (!updateUserEmailConfirmationStatus.Succeeded)
                {
                    // updating user's email confirmation status failed, adding erros to ModelState
                    foreach (IdentityError error in updateUserEmailConfirmationStatus.Errors)
                        ActionContext.ModelState.AddModelError(string.Empty, error.Description);

                    return redirectRoute;
                }
            }

            ApplicationUser prospectiveUser = existingUser ?? new ApplicationUser()
            {
                Email = inputModel.Email,
                UserName = inputModel.Email,
                FirstName = inputModel.FirstName,
                LastName = inputModel.LastName
            };

            // if user doesn't already exist, create new user without password
            IdentityResult createUser = existingUser == null ? await UserManager.CreateAsync(prospectiveUser) : null;

            // if user already registered with email or user created successfully now, send verification code to confirm email
            if (existingUser != null || createUser.Succeeded)
            {
                // generate TOTP based verification code for confirming the user's email
                string verificationCode = await UserManager.GenerateTwoFactorTokenAsync(prospectiveUser, TokenOptions.DefaultEmailProvider);

                // user account successfully created, verify email
                string resendEmailRecordId = await EmailService.SendEmailConfirmationEmail(AutomatedEmails.NoReply, inputModel.Email, inputModel.Email, verificationCode);

                TempData[TempDataKeys.UserEmail] = prospectiveUser.Email;
                TempData[TempDataKeys.ResendEmailRecordId] = resendEmailRecordId;

                redirectRoute = GenerateRouteUrl("ConfirmEmail", "SignUp", "Enroll");
            }
            else if (!createUser.Succeeded)
            {
                // new user creation failed, adding erros to ModelState
                foreach (IdentityError error in createUser.Errors)
                    ActionContext.ModelState.AddModelError(string.Empty, error.Description);
            }

            return redirectRoute;
        }

        public async Task<object[]> ManageEmailConfirmation()
        {
            object[] result = await IdentityService.ManageEmailChallenge(RootRoute);

            return result;
        }

        public async Task<string> VerifyEmailConfirmation(EmailChallengeInputModel inputModel)
        {
            string redirectRoute = null;

            if (!ActionContext.ModelState.IsValid)
                return redirectRoute;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                redirectRoute = RootRoute;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
                
                redirectRoute = RootRoute;
            }
            else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
            {
                // user exists with unregistered account and unconfirmed email, so user is signing up
                // or
                // user exists with registered account and confirmed email, so user is either signing in or trying to reset TOTP access

                // if TOTP code verified, redirect to target page
                bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    inputModel.VerificationCode
                );

                if (totpCodeVerified)
                {
                    redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        inputModel.ResendEmailRecordId,
                        UserActionContexts.ConfirmEmailChallenge,
                        null
                    );
                }
                else
                {
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                }
            }

            return redirectRoute;
        }

        public async Task<object[]> RegisterTOTPAccess()
        {
            RegisterTOTPAccessInputModel model = null;
            string redirectRoute = RootRoute;
            object[] result = GenerateArray(model, redirectRoute);

            bool userEmailExists = TempData.TryGetValue(TempDataKeys.UserEmail, out object userEmailTempData);

            string userEmail = userEmailExists ? userEmailTempData.ToString() : null;
            if (string.IsNullOrWhiteSpace(userEmail))
                return result;
            
            // retain TempData so page reload keeps user on the same page
            TempData.Keep();

            ApplicationUser user = await UserManager.FindByEmailAsync(userEmail);

            if (user == null || !user.EmailConfirmed)
            {
                // user does not exist or user's email was never confirmed
                return result;
            }
            else if (user.AccountRegistered && !user.RequiresAuthenticatorReset)
            {
                // user is completely registered but does not require authenticator reset

                redirectRoute = GenerateRouteUrl("Prompt", "ResetTOTPAccess", "Access");

                result = GenerateArray(model, redirectRoute);
            }
            else if (!user.AccountRegistered || (user.AccountRegistered && user.RequiresAuthenticatorReset))
            {
                // user is not completely registered yet or user is registered & needs to reset authenticator

                string authenticatorKey = null;
                string authenticatorKeyUri = null;

                IdentityResult resetAuthenticatorKey = await UserManager.ResetAuthenticatorKeyAsync(user);

                if (resetAuthenticatorKey.Succeeded)
                {
                    authenticatorKey = await UserManager.GetAuthenticatorKeyAsync(user);
                    authenticatorKeyUri = IdentityService.GenerateQRCodeUri(userEmail, authenticatorKey);
                }
                else
                {
                    // resetting authenticator key failed, user will be redirected to defaultRoute
                    return result;
                }

                // create a session TOTP code valid for 3 mins - when user surpasses 3 mins to scan & submit the TOTP code, request will fail
                string sessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

                model = new RegisterTOTPAccessInputModel()
                {
                    AuthenticatorKey = authenticatorKey,
                    AuthenticatorKeyUri = authenticatorKeyUri,
                    Email = userEmail,
                    SessionVerificationTOTPCode = sessionVerificationCode,
                };

                result = GenerateArray(model, redirectRoute);
            }

            return result;
        }

        public async Task<string> VerifyTOTPAccessRegistration(RegisterTOTPAccessInputModel inputModel)
        {
            string redirectRoute = null;

            if (!ActionContext.ModelState.IsValid)
                return redirectRoute;
            
            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null || !user.EmailConfirmed)
            {
                // user doesn't exist, redirect to root route
                redirectRoute = RootRoute;
            }
            else if (user.AccountRegistered && !user.RequiresAuthenticatorReset)
            {
                // user exists but doesn't require to reset authenticator, redirect to reset authenticator prompt page
                redirectRoute = GenerateRouteUrl("Prompt", "ResetTOTPAccess", "Access");
            }
            else if (!user.AccountRegistered || (user.AccountRegistered && user.RequiresAuthenticatorReset))
            {
                // user needs to register TOTP authenticator to complete account registration
                // or
                // user has a registered account and needs to reset TOTP authenticator

                bool sessionVerified = await UserManager.VerifyTwoFactorTokenAsync(
                    user,
                    CustomTokenOptions.GenericTOTPTokenProvider,
                    inputModel.SessionVerificationTOTPCode
                );

                if (sessionVerified)
                {
                    bool TOTPAccessVerified = await UserManager.VerifyTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultAuthenticatorProvider,
                        inputModel.TOTPCode
                    );

                    string newSessionVerificationCode = null;

                    if (TOTPAccessVerified)
                    {
                        user.TwoFactorEnabled = true;
                        user.AccountRegistered = true;
                        user.RequiresAuthenticatorReset = false;

                        IdentityResult updateUser = await UserManager.UpdateAsync(user);

                        if (updateUser.Succeeded)
                        {
                            // account registration complete, sign in the user
                            redirectRoute = await IdentityService.SignIn(user);
                        }
                        else
                        {
                            // update user failed but session still valid, generate new session verification code
                            newSessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

                            Console.WriteLine("Update user failed when registering TOTP Access");

                            // Error enabling Two Factor Authentication, add them to ModelState
                            foreach (IdentityError error in updateUser.Errors)
                                Console.WriteLine(error.Description);
                            
                            ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
                        }
                    }
                    else
                    {
                        // session still valid, generate new session verification code
                        newSessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

                        // TOTP access verification failed, adding erros to ModelState
                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid TOTP code");
                    }

                    if (newSessionVerificationCode != null)
                        inputModel.SessionVerificationTOTPCode = newSessionVerificationCode;
                }
                else
                {
                    // Session verification failed, redirecting to RootRoute
                    redirectRoute = RootRoute;
                }
            }

            return redirectRoute;
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
