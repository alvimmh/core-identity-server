using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Storage;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class ResetTOTPAccessService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private EmailService EmailService;
        private IdentityService IdentityService;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public ResetTOTPAccessService(
            IActionContextAccessor actionContextAccessor,
            IConfiguration configuration,
            EmailService emailService,
            IdentityService identityService,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            UserManager<ApplicationUser> userManager
        ) : base(actionContextAccessor, configuration, tempDataDictionaryFactory)
        {
            EmailService = emailService;
            IdentityService = identityService;
            UserManager = userManager;

            RootRoute = GenerateRouteUrl("ManageAuthenticator", "ResetTOTPAccess", "Access");
        }

        /// <summary>
        ///     public async Task<ManageAuthenticatorViewModel> ManageAuthenticator()
        ///     
        ///     Manages the ManageAuthenticator GET action.
        ///     
        ///     1. Fetches the user by calling the UserManager.GetUserAsync() method.
        ///     
        ///     2. If user is not found, the method returns null. If found, the method counts the
        ///         remaining TOTP access recovery codes for the user and returns a view model
        ///             containing this information.
        /// </summary>
        /// <returns>
        ///     A view model (ManageAuthenticatorViewModel) containing the number of
        ///         remaining TOTP access recovery codes
        ///             or,
        ///                 null.
        /// </returns>
        public async Task<ManageAuthenticatorViewModel> ManageAuthenticator()
        {
            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
                return null;

            int recoveryCodesLeft = await UserManager.CountRecoveryCodesAsync(user);

            return new ManageAuthenticatorViewModel { RecoveryCodesLeft = recoveryCodesLeft };
        }


        /// <summary>
        ///     public async Task<string> InitiateTOTPAccessRecoveryChallenge(
        ///         InitiateTOTPAccessRecoveryChallengeInputModel inputModel)
        ///         
        ///     Manages the InitiateUnauthenticatedRecovery and InitiateAuthenticatedRecovery POST actions.
        ///     
        ///     1. Checks if the user is signed in. If the user is signed in, the user is fetched using
        ///         the UserManager.GetUserAsync() method. If the user could not be found, the method
        ///             returns the RootRoute for this controoler.
        ///     
        ///     2. Checks if the ModelState is valid. If not, the method returns the RootRoute. If valid,
        ///         the method continues.
        ///     
        ///     3. If the user was not signed in in step 1, the user is fetched from the email property in the
        ///         input model using the UserManager.FindByEmailAsync() method.
        ///         
        ///     4. If the user is still not found or if the user's email was not confirmed, the method returns
        ///         the RootRoute.
        ///         
        ///     5. If the user did not complete registering their account, an email is sent to the user to
        ///         request them to complete registration. Then the method returns the RootRoute.
        ///         
        ///     6. If the user did complete account registration, then all previous TempData are cleared. If
        ///         the user is not signed in, the user email is stored in the TempData so it persists on page
        ///             change. Then the method returns a route to the Recover TOTP Access Challenge page.
        ///             
        ///     7. For all other conditions, the method returns the RootRoute.
        /// </summary>
        /// <param name="inputModel">The input model containing the user's email</param>
        /// <returns>The route to redirect the application</returns>
        public async Task<string> InitiateTOTPAccessRecoveryChallenge(InitiateTOTPAccessRecoveryChallengeInputModel inputModel)
        {
            ApplicationUser user = null;
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user == null)
                    return RootRoute;
            }
            else if (!ActionContext.ModelState.IsValid)
            {
                return RootRoute;
            }

            user = user ?? await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null || !user.EmailConfirmed)
            {
                // user doesn't exist or user email is not confirmed
                return RootRoute;
            }
            else if (!user.AccountRegistered)
            {
                // user exists but did not complete account registration, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, inputModel.Email, user.UserName);

                return RootRoute;
            }
            else if (user.AccountRegistered)
            {
                // clear all unnecessary tempdata
                TempData.Clear();

                if (!currentUserSignedIn)
                    TempData[TempDataKeys.UserEmail] = user.Email;

                string redirectRoute = GenerateRouteUrl("RecoverTOTPAccessChallenge", "ResetTOTPAccess", "Access");

                return redirectRoute;
            }

            return RootRoute;
        }


        /// <summary>
        ///     public async Task<object[]> ManageTOTPAccessRecoveryChallenge()
        ///     
        ///     Manages the RecoverTOTPAccessChallenge GET action.
        ///     
        ///     1. Delegates the task to the IdentityService.ManageTOTPAccessRecoveryChallenge() method.
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
        public async Task<object[]> ManageTOTPAccessRecoveryChallenge()
        {
            object[] result = await IdentityService.ManageTOTPAccessRecoveryChallenge(RootRoute);

            return result;
        }


        /// <summary>
        ///     public async Task<string> ManageTOTPAccessRecoveryChallengeVerification(
        ///         TOTPAccessRecoveryChallengeInputModel inputModel)
        ///         
        ///     Manages the RecoverTOTPAccessChallenge POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns null.
        ///     
        ///     2. If valid, the user is fetched using the UserManager.FindByEmailAsync() method.
        ///     
        ///     3. If the user is null, the method returns the RootRoute.
        ///     
        ///     4. If the user's email is confirmed but the user did not complete account
        ///         registration, an email is sent to the user to request them to complete registration
        ///             and the method returns the RootRoute.
        ///             
        ///     5. If the user's email is confirmed and the user did complete account registration,
        ///         the TOTP access recovery code submitted by the user is verified by calling the
        ///             IdentityService.VerifyTOTPAccessRecoveryCode() method.
        ///             
        ///     6. If the recovery code verification succeeded, then a redirect route is determined
        ///         by calling the IdentityService.ManageTOTPChallengeSuccess() method. And the
        ///             method returns this redirect route.
        ///             
        ///     7. If the recovery code verification failed, then an error message is added to the
        ///         ModelState and null is returned from the method.
        ///         
        ///     8. And for all other scenarios, the method returns null.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model containing the user's email, TOTP code and the return url.
        /// </param>
        /// <returns>
        ///     A route to redirect the application if TOTP code verification succeeded
        ///         or,
        ///             null if verification failed.
        /// </returns>
        public async Task<string> ManageTOTPAccessRecoveryChallengeVerification(TOTPAccessRecoveryChallengeInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
            {
                return null;
            }

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                return RootRoute;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
                
                return RootRoute;
            }
            else if (user.EmailConfirmed && user.AccountRegistered)
            {
                // user exists with registered account and confirmed email, so user trying to reset TOTP access

                bool totpAccessRecoveryCodeVerified = await IdentityService.VerifyTOTPAccessRecoveryCode(
                    user, inputModel.VerificationCode
                );

                if (totpAccessRecoveryCodeVerified)
                {
                    string redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        null,
                        UserActionContexts.TOTPAccessRecoveryChallenge,
                        null
                    );

                    return redirectRoute;
                }
                else
                {
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid recovery code");

                    return null;
                }
            }

            return null;
        }


        /// <summary>
        ///     public async Task<object[]> ManageResetTOTPAccessRecoveryCodes()
        ///     
        ///     Manages the ResetTOTPAccessRecoveryCodes GET action.
        ///     
        ///     1. Fetches the user by calling the UserManager.GetUserAsync() method.
        ///     
        ///     2. If the user is not found, the method returns an array of objects
        ///         containing null and the RootRoute.
        ///         
        ///     3. If the user is found, a view model is created containing the user's id. And
        ///         the method returns this view model and null in an array of objects.
        /// </summary>
        /// <returns>
        ///     An array of objects, containing
        ///         the view model and null if the user is found
        ///             or,
        ///                 null and the RootRoute if user cound not be found.
        /// </returns>
        public async Task<object[]> ManageResetTOTPAccessRecoveryCodes()
        {
            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user != null)
            {
                ResetTOTPAccessRecoveryCodesInputModel viewModel = new ResetTOTPAccessRecoveryCodesInputModel() { Id = user.Id };
            
                return GenerateArray(viewModel, null);
            }
            
            return GenerateArray(null, RootRoute);
        }


        /// <summary>
        ///     public async Task<string> ResetTOTPAccessRecoveryCodes(
        ///         ResetTOTPAccessRecoveryCodesInputModel inputModel)
        ///         
        ///     Manages the ResetTOTPAccessRecoveryCodes POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns the RootRoute.
        ///     
        ///     2. If valid, the user is fetched using the UserManager.GetUserAsync() method.
        ///     
        ///     3. If the user is not found, the method returns the RootRoute.
        ///     
        ///     4. If the user is found and the user's id matches that of the input model, then
        ///         all the user's TOTP access recovery codes are revoked by calling the
        ///             UserManager.GenerateNewTwoFactorRecoveryCodesAsync() method while supplying
        ///                 this method with 0 as the number of codes to be generated. Finally,
        ///                     the method returns a redirect route to the Register TOTP Access Successful page
        ///                         which will generate new TOTP access recovery codes for the user.
        /// </summary>
        /// <param name="inputModel">The input model containing the user's id</param>
        /// <returns>The route to redirect the application</returns>
        public async Task<string> ResetTOTPAccessRecoveryCodes(ResetTOTPAccessRecoveryCodesInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return RootRoute;

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user != null && user.Id == inputModel.Id)
            {
                // unused variable
                // note, the UserManager.GenerateNewTwoFactorRecoveryCodesAsync() method is not
                // generating any new recovery codes. Its only revoking existing ones.
                IEnumerable<string> userTOTPRecoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);

                return GenerateRouteUrl("RegisterTOTPAccessSuccessful", "SignUp", "Enroll", "resetaccess=true");
            }

            return RootRoute;
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
