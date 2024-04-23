using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Enroll.Models.SignUp;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Storage;
using System.Collections.Generic;

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


        /// <summary>
        ///     public async Task<string> RegisterProspectiveUser(ProspectiveUserInputModel inputModel)
        ///     
        ///     Manages the RegisterProspectiveUser POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns null.
        ///         If valid, tries to fetch a user with the email address provided
        ///             in the input model.
        ///     
        ///     2. If a matching user is found, and that user has completed
        ///             account registration, the method returns a redirect route to the
        ///                 Reset TOTP Access page.
        ///                 
        ///     4. If a matching user is found and that user has confirmed their email,
        ///         it means the user did not complete account registration process.
        ///             Otherwise the method execution would have entered the previous
        ///                 'if' block in step 3.
        ///                 
        ///        Since the user did not complete account registration,
        ///         the user's EmailConfirmed property is updated to false. This will help
        ///             the user to complete account registration. If this update fails,
        ///                 all errors are added to the ModelState and the method returns
        ///                     null. If the update succeeds, the method continues.
        ///             
        ///     5. A new variable is used to hold (i) the matched user found using the email address
        ///         or (ii) a new ApplicationUser object, created using the information in the input
        ///         model, in case a matching user was not found.
        ///         
        ///     6. In case a matching user was not found, the new ApplicationUser object is persisted
        ///         in the database using the UserManager.CreateAsync() method.
        ///         
        ///     7. A verification code is generated to confirm the user's email in either case, 5(i)
        ///         or 5(ii). This code is sent to the user's email address so they may continue
        ///             registering their account by using this code.
        ///        
        ///        And since the user is not logged in yet, the user's email and the email record id
        ///         of the email sent to the user are stored in TempData. Then the method returns a
        ///             redirect route to the Confirm Email page. The data stored in the TempData
        ///                 will be used in this page to populate the input model.
        ///                                 
        ///     8. In case persisting the new ApplicationUser failed in step 6, all errors are added
        ///         to the ModelState and the method returns null.
        ///         
        ///     9. The method also returns null for all other scenarios.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model object containing the user's email address, first name and last name.
        /// </param>
        /// <returns>
        ///     A route to redirect the application
        ///         or,
        ///             null.
        /// </returns>
        public async Task<string> RegisterProspectiveUser(ProspectiveUserInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;

            ApplicationUser existingUser  = await UserManager.FindByEmailAsync(inputModel.Email);

            if (existingUser != null && existingUser.AccountRegistered)
            {
                string redirectRoute = GenerateRouteUrl("ManageAuthenticator", "ResetTOTPAccess", "Access");

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

                    return null;
                }
            }

            ApplicationUser prospectiveUser = existingUser ?? new ApplicationUser()
            {
                Email = inputModel.Email,
                UserName = inputModel.Email,
                FirstName = inputModel.FirstName,
                LastName = inputModel.LastName,
                CreatedAt = DateTime.UtcNow
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

                string redirectRoute = GenerateRouteUrl("ConfirmEmail", "SignUp", "Enroll");

                return redirectRoute;
            }
            else if (!createUser.Succeeded)
            {
                // new user creation failed, adding erros to ModelState
                foreach (IdentityError error in createUser.Errors)
                    ActionContext.ModelState.AddModelError(string.Empty, error.Description);

                return null;
            }

            return null;
        }


        /// <summary>
        ///     public async Task<object[]> ManageEmailConfirmation()
        ///     
        ///     Manages the ConfirmEmail GET action.
        ///     
        ///     1. Delegates the task to the IdentityService.ManageEmailChallenge() method.
        ///     
        ///     2. Returns the array of objects containing a view model and null
        ///         or
        ///             null and a redirect route.
        /// </summary>
        /// <returns>
        ///     Returns an array of objects containing the view model and null
        ///         or,
        ///             null and a redirect route.
        /// </returns>
        public async Task<object[]> ManageEmailConfirmation()
        {
            object[] result = await IdentityService.ManageEmailChallenge(RootRoute);

            return result;
        }


        /// <summary>
        ///     public async Task<string> VerifyEmailConfirmation(EmailChallengeInputModel inputModel)
        ///     
        ///     Manages the ConfirmEmail POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns null. If valid,
        ///         a user is fetched with the email address in the input model.
        ///         
        ///     2. If the user was not found, the method returns the RootRoute - the Register
        ///         Prospective User page route.
        ///     
        ///     3. If the user was found and the user had their email confirmed but did not complete
        ///         account registration, an email is sent to the user to complete account
        ///             registration and the method returns the RootRoute. And an error message
        ///                 is set in the TempData for the user to see in the RootRoute.
        ///                 
        ///     4. If the user was found, and the user neither confirmed their email nor completed
        ///         account registration, then the user submitted TOTP code from the input model is
        ///             verified using the IdentityService.VerifyTOTPCode() method.
        ///             
        ///        If verification failed, an error message is added to the ModelState for the user
        ///         and the method returns null.
        ///         
        ///        If verification succeeded, the IdentityService.ManageTOTPChallengeSuccess()
        ///         method is used to determine the redirect route for the application, which is
        ///             returned from the current method.
        ///             
        ///     5. The method returns null for all other scenarios.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model containing the TOTP code submitted by the user, user's email
        ///         address, the email record id for the resend option for the email that was
        ///             used to send the TOTP code to the user and a return url.
        /// </param>
        /// <returns>
        ///     The route to redirect the application
        ///         or,
        ///             null.
        /// </returns>
        public async Task<string> VerifyEmailConfirmation(EmailChallengeInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                return RootRoute;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                TempData[TempDataKeys.ErrorMessage] = "An error occured. Please check email for further instructions.";

                // user exists with unregistered account but confirmed email, send email to complete registration
                // so user goes through the registration process again
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                return RootRoute;
            }
            else if (!user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with unconfirmed email and unregistered account, so user is signing up

                // if TOTP code verified, redirect to target page
                bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                    user,
                    TokenOptions.DefaultEmailProvider,
                    inputModel.VerificationCode
                );

                if (!totpCodeVerified)
                {
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");

                    return null;
                }
                else
                {
                    string redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        inputModel.ResendEmailRecordId,
                        UserActionContexts.ConfirmEmailChallenge,
                        null
                    );

                    return redirectRoute;
                }
            }

            return null;
        }


        /// <summary>
        ///     public async Task<object[]> RegisterTOTPAccess()
        ///     
        ///     Manages the RegisterTOTPAccess GET action.
        ///     
        ///     1. Tries to retrieve the user email from the TempData. If it is not found,
        ///         the method returns an array of objects containing null and the RootRoute.
        ///         
        ///     2. Tries to retrieve the DateTime for the key TempDataKeys.TempDataExpiryDateTime
        ///         from the TempData. This DateTime indicates the point of time until which the
        ///             user is allowed to use any existing TempData i.e., the user's email.
        ///             
        ///        This is strictly a security measure to stop unwanted registration of TOTP access.
        ///         Expiring TempData helps against unwanted compromise of TOTP Access, when user
        ///             leaves screen/browser unattended or stays in the page for more than
        ///                 AccountOptions.TempDataLifetimeInSeconds seconds and then tries to refresh
        ///                     the page. This measure only protects against refreshing the page via
        ///                         GET action that could increase the TOTP Access registration time
        ///                             window.
        ///                     
        ///        If TempData is expired, user can no longer refresh to register TOTP Access authenticator,
        ///         instead user will be redirected to RootRoute.
        ///         
        ///        If it is not found or if the DateTime has surpassed the current UTC time, the
        ///         method returns an array of objects containing null and the RootRoute.
        ///         
        ///        If it is found and the DateTime has not surpassed the current UTC time, the
        ///         TempData is kept so it is persisted on page change and the method continues.
        ///         
        ///     3. If the user is allowed to use the TempData values i.e., TempDataKeys.UserEmail,
        ///         the user data is fetched by using the UserManager.FindByEmailAsync() method.
        ///         
        ///     4. In case the user was not found or if the user's email address is not confirmed,
        ///         the method returns an array of objects containing null and RootRoute.
        ///         
        ///     5. If the user is found and the user has completed account registration but user
        ///         does not require an authenticator reset, then user has come to the wrong
        ///             controller/action and needs to be rerouted. The method returns an array
        ///                 of objects containing null and a redirect route to the Manage
        ///                     Authenticator page.
        ///                     
        ///     6. If the user is found, and (i) the user has not completed account registration, or
        ///         (ii) the user has completed account registration and requires an authenticator
        ///             reset, then the user's authenticator key is reset using the
        ///                 UserManager.ResetAuthenticatorKeyAsync() method.
        ///                 
        ///        If the reset failed, an error message is set in the TempData for the user to see.
        ///         And the method returns an array of objects containing null and the RootRoute.
        ///         
        ///     7. If the reset succeeded, the user's authenticator key is retrieved using the
        ///         UserManager.GetAuthenticatorKeyAsync() method. Additionally, this key is then
        ///             converted to a QR code creatable uri using the method
        ///                 IdentityService.GenerateQRCodeUri().
        ///                 
        ///     8. A new session verification TOTP code is generated using the
        ///         UserManager.GenerateTwoFactorTokenAsync() method. This code and its verification
        ///             remains behind the scenes and helps with protecting the user's session as
        ///                 the user is still not signed in and important data is persisted on
        ///                     page change using TempData.
        ///                     
        ///     9. A RegisterTOTPAccessInputModel object is created and the method returns an array
        ///         of objects containing this input model and null. The view for the Register TOTP
        ///             Access page will use this model to populate data on the page.
        ///             
        ///     10. For all cases other than steps 4, 5 or 6, the method returns an array of objects
        ///         containing null and the RootRoute.
        /// </summary>
        /// <returns>
        ///     An array of objects containing,
        ///         (i) the view/input model and null for the successful scenario
        ///             or,
        ///                 (ii) null and a redirect route for the failed scenario.
        /// </returns>
        public async Task<object[]> RegisterTOTPAccess()
        {
            bool userEmailExists = TempData.TryGetValue(TempDataKeys.UserEmail, out object userEmailTempData);

            string userEmail = userEmailExists ? userEmailTempData.ToString() : null;

            if (string.IsNullOrWhiteSpace(userEmail))
                return GenerateArray(null, RootRoute);

            bool tempDataExpiryDateTimeExists = TempData.TryGetValue(TempDataKeys.TempDataExpiryDateTime, out object tempdataExpiryDateTimeTempData);

            DateTime? tempDataExpiryDateTime = tempDataExpiryDateTimeExists ? (DateTime)tempdataExpiryDateTimeTempData : null;

           /*
            * if TempData is not expired, retain TempData so page reload keeps user on the same page
            *
            * expiring TempData helps against unwanted compromise of TOTP Access, when user lefts
            * screen/browser unattended or stays in the page for more than 3 minutes and then tries to
            * refresh the page 
            *
            * if TempData is expired, user can no longer refresh to register TOTP Access authenticator,
            * instead user will be redirected to RootRoute
            *
            * this only protects against refreshing the page via GET action that could increase the
            * TOTP Access registration time window
            */
            if (tempDataExpiryDateTime != null && DateTime.UtcNow < tempDataExpiryDateTime)
                TempData.Keep();
            else
                return GenerateArray(null, RootRoute);

            ApplicationUser user = await UserManager.FindByEmailAsync(userEmail);

            if (user == null || !user.EmailConfirmed)
            {
                // user does not exist or user's email is not confirmed
                return GenerateArray(null, RootRoute);
            }
            else if (user.AccountRegistered && !user.RequiresAuthenticatorReset)
            {
                // user is completely registered but does not require authenticator reset

                string redirectRoute = GenerateRouteUrl("ManageAuthenticator", "ResetTOTPAccess", "Access");

                return GenerateArray(null, redirectRoute);
            }
            else if (!user.AccountRegistered || (user.AccountRegistered && user.RequiresAuthenticatorReset))
            {
                // user is not completely registered yet or user is registered & needs to reset authenticator

                IdentityResult resetAuthenticatorKey = await UserManager.ResetAuthenticatorKeyAsync(user);

                if (!resetAuthenticatorKey.Succeeded)
                {
                    TempData[TempDataKeys.ErrorMessage] = "An error occured, please try again.";

                    // resetting authenticator key failed, user will be redirected to defaultRoute
                    return GenerateArray(null, RootRoute);
                }
                else
                {
                    string authenticatorKey = await UserManager.GetAuthenticatorKeyAsync(user);
                    string authenticatorKeyUri = IdentityService.GenerateQRCodeUri(userEmail, authenticatorKey);
                
                    // create a session TOTP code valid for 3 mins - when user surpasses 3 mins to scan & submit the TOTP code, request will fail
                    string sessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

                    RegisterTOTPAccessInputModel model = new RegisterTOTPAccessInputModel()
                    {
                        AuthenticatorKey = authenticatorKey,
                        AuthenticatorKeyUri = authenticatorKeyUri,
                        Email = userEmail,
                        SessionVerificationTOTPCode = sessionVerificationCode
                    };

                    return GenerateArray(model, null);
                }
            }

            return GenerateArray(null, RootRoute);
        }


        /// <summary>
        ///     public async Task<string> VerifyTOTPAccessRegistration(RegisterTOTPAccessInputModel inputModel)
        ///     
        ///     Manages the RegisterTOTPAccess POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns null. If valid,
        ///         the user is fetched using the UserManager.FindByEmailAsync() method.
        ///         
        ///     2. If the user was not found or if the user did not confirm their email
        ///         address, the method returns the RootRoute.
        ///         
        ///     3. If the user was found, and the user completed account registration but does not
        ///         require an authenticator reset, the user is in the wrong place and needs to be
        ///             rerouted. The method returns the Manage Authenticator page route.
        ///             
        ///     4. If the user was found, and (i) the user did not complete account registration
        ///         or, (ii) the user completed account registration and requires an authenticator
        ///             reset, it means (i) the user needs to register TOTP authenticator to complete
        ///                 account registration or (ii) user has a registered account and needs to
        ///                     reset TOTP authenticator.
        ///                     
        ///        The session verification code is verified first. This session verification code works
        ///         behind the scene and protects the user's current session as the user is still not
        ///             logged in. Data is persisted only using the TempData feature.
        ///                         
        ///     5. The method returns null for all scenarios other than steps 2, 3, or 4.
        ///                         
        ///     6. If session verification failed, an error message is set in the TempData
        ///         for the user. And the method returns the RootRoute.
        ///         
        ///     7. In case the session verification succeeded, the TOTP code submitted by the user
        ///         in the input model is verified using the UserManager.VerifyTwoFactorTokenAsync()
        ///             method.
        ///             
        ///     8. If the TOTP code verification failed, the session verification code described
        ///         in step 4 is regenerated using the UserManager.GenerateTwoFactorTokenAsync() method
        ///             and reset in the input model. An erro message is added to the ModelState
        ///                 for the user and the method returns null.
        ///                 
        ///     9. If the TOTP code verification succeeded, the user's AccountRegistered property is
        ///         set to true and RequiresAuthenticatorReset property is set to false. Then these
        ///             changes are saved to the database to update the user using the
        ///                 UserManager.UpdateAsync() method.
        ///                 
        ///     10. If the update failed, the session verification code is regenerated using the
        ///         UserManager.GenerateTwoFactorTokenAsync() method and the code is reset in the
        ///             input model. All errors due to the failure are printed to the console. An
        ///                 error message is added to the ModelState for the user. And the method
        ///                     returns null.
        ///                     
        ///     11. If the update succeeded, the sign in prerequisites are checked for the user
        ///         using the IdentityService.VerifySignInPrerequisites() method.
        ///         
        ///        In case the user can sign in, the user is signed in using the IdentityService.SignIn()
        ///         method which returns a redirect route and this route is overwritten now to redirect
        ///             to the Register TOTP Access Successful page. Then the method returns this redirect
        ///                 route. In case the IdentityService.SignIn() method failed, it will return null
        ///                     and the method will also return null.
        ///                                 
        ///        If the user cannot sign in, the method returns a redirect route to the Sign In page
        ///         and a successful message is added to the TempData to show the user on the Sign In
        ///             page.
        /// </summary>
        /// <param name="inputModel">
        ///     The RegisterTOTPAccessInputModel object containing the user's authenticator key,
        ///         authenticator key uri, email address, TOTP code and the session verification code.
        /// </param>
        /// <returns>
        ///     The route to redirect the application or null to return back to the current page
        /// </returns>
        public async Task<string> VerifyTOTPAccessRegistration(RegisterTOTPAccessInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;
            
            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null || !user.EmailConfirmed)
            {
                // user doesn't exist or user's email is not confirmed, redirect to root route
                return RootRoute;
            }
            else if (user.AccountRegistered && !user.RequiresAuthenticatorReset)
            {
                // user exists but doesn't require to reset authenticator, redirect to manage authenticator page
                return GenerateRouteUrl("ManageAuthenticator", "ResetTOTPAccess", "Access");
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

                if (!sessionVerified)
                {
                    TempData[TempDataKeys.ErrorMessage] = "An error occured. Please try again.";

                    // Session verification failed, redirecting to RootRoute
                    return RootRoute;
                }
                else
                {
                    bool TOTPAccessVerified = await UserManager.VerifyTwoFactorTokenAsync(
                        user,
                        TokenOptions.DefaultAuthenticatorProvider,
                        inputModel.TOTPCode
                    );

                    if (!TOTPAccessVerified)
                    {
                        // session still valid, generate new session verification code
                        string renewedSessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(
                            user, CustomTokenOptions.GenericTOTPTokenProvider);

                        inputModel.SessionVerificationTOTPCode = renewedSessionVerificationCode;

                        // TOTP access verification failed, adding erros to ModelState
                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid TOTP code");

                        return null;
                    }
                    else
                    {
                        user.AccountRegistered = true;
                        user.RequiresAuthenticatorReset = false;

                        IdentityResult updateUser = await UserManager.UpdateAsync(user);

                        if (!updateUser.Succeeded)
                        {
                            // update user failed, user can retry

                            // session still valid, generate new session verification code
                            string renewedSessionVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(
                                user, CustomTokenOptions.GenericTOTPTokenProvider);

                            inputModel.SessionVerificationTOTPCode = renewedSessionVerificationCode;

                            Console.WriteLine("Update user failed when registering TOTP Access");

                            // log errors
                            foreach (IdentityError error in updateUser.Errors)
                                Console.WriteLine(error.Description);

                            // Error enabling Two Factor Authentication, add them to ModelState
                            ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");

                            return null;
                        }
                        else
                        {
                            bool userCanSignIn = await IdentityService.VerifySignInPrerequisites(user);

                            if (userCanSignIn)
                            {
                                // account registration complete, sign in the user
                                string redirectRoute = await IdentityService.SignIn(user);

                                if (redirectRoute != null)
                                    redirectRoute = GenerateRouteUrl("RegisterTOTPAccessSuccessful", "SignUp", "Enroll");

                                return redirectRoute;
                            }
                            else
                            {
                                TempData[TempDataKeys.SuccessMessage] = "TOTP Access Registration Successful. Please sign in to continue.";

                                return GenerateRouteUrl("SignIn", "Authentication", "Access");
                            }
                        }
                    }
                }
            }

            return null;
        }


        /// <summary>
        ///     public async Task<object[]> ManageTOTPAccessSuccessfulRegistration(bool resetAccess = false)
        ///     
        ///     Manages the RegisterTOTPAccessSuccessful GET action.
        ///     
        ///     1. Fetches the user using the UserManager.GetUserAsync() method.
        ///     
        ///     2. If user is not found, the method returns an array of objects containing null and
        ///         RootRoute.
        ///         
        ///     3. If user is found, checks if there is at least 1 TOTP access recovery code available
        ///         for the user. If not, then 3 new TOTP access recovery codes are generated for the
        ///             user using the IdentityService.GenerateTOTPRecoveryCodes() method and a
        ///                 view model containing the new recovery codes is created. Finally, the method
        ///                     returns an array of objects containing the view model and null.
        /// </summary>
        /// <param name="resetAccess">
        ///     A boolean indicating whether the corresponding GET action is due to a new account
        ///         creation or just a reset of TOTP access for an existing user.
        /// </param>
        /// <returns>
        ///     An array of objects containing
        ///         the view model and null for a successful scenario
        ///             or,
        ///                 null and a redirect route for a failed scenario.
        /// </returns>
        public async Task<object[]> ManageTOTPAccessSuccessfulRegistration(bool resetAccess = false)
        {
            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user != null)
            {
                List<string> userTOTPRecoveryCodes = null;

                int validRecoveryCodes = await UserManager.CountRecoveryCodesAsync(user);

                if (validRecoveryCodes < 1)
                    userTOTPRecoveryCodes = await IdentityService.GenerateTOTPRecoveryCodes(user, 3);

                RegisterTOTPAccessSuccessfulViewModel viewModel = new RegisterTOTPAccessSuccessfulViewModel()
                {
                    TOTPRecoveryCodes = userTOTPRecoveryCodes != null ? string.Join(", ", userTOTPRecoveryCodes) : null,
                    ResetAccess = resetAccess
                };

                return GenerateArray(viewModel, null);
            }

            return GenerateArray(null, RootRoute);
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
