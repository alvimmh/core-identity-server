using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Constants.Emails;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Constants.Storage;
using CoreIdentityServer.Internals.Constants.Account;
using IdentityModel;
using System.Linq;

namespace CoreIdentityServer.Internals.Services.Identity.IdentityService
{
    public class IdentityService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly SignInManager<ApplicationUser> SignInManager;
        private EmailService EmailService;
        private readonly UrlEncoder UrlEncoder;
        private bool ResourcesDisposed;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            UrlEncoder urlEncoder
        ) : base(actionContextAccessor, tempDataDictionaryFactory)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            EmailService = emailService;
            UrlEncoder = urlEncoder;
        }


        /// <summary>
        ///     public async Task<object[]> ManageTOTPAccessRecoveryChallenge(string defaultRoute)
        ///     
        ///     Manages the RecoverTOTPAccessChallenge GET action for the
        ///         ResetTOTPAccessService.ManageTOTPAccessRecoveryChallenge() method.
        ///         
        ///     1. Checks if the current user is signed in. If signed in, the user is fetched using
        ///         the UserManager.GetUserAsync() method.
        ///         
        ///     2. If not signed in, the user email is retreived from the TempData.
        ///     
        ///     3. In case the user or the user's email was found, a view model is created by
        ///         calling the GenerateTOTPAccessRecoveryChallengeInputModel() method which. And all
        ///             existing TempData are kept so they persist on page reload. Finally, the method
        ///                 returns an array of objects containing the view model and null.
        ///                 
        ///     4. In case the user or the user's email was not found, the method returns an array of
        ///         objects containing null and the defaultRoute param.
        /// </summary>
        /// <param name="defaultRoute">
        ///     The default route to redirect the application in case the user or user email was not found.
        /// </param>
        /// <returns>An array of objects containing
        ///     the view model and null
        ///         or,
        ///             null and the default route param.
        /// </returns>
        public async Task<object[]> ManageTOTPAccessRecoveryChallenge(string defaultRoute)
        {
            // check if user is signed in
            bool userEmailExists = false;
            bool currentUserSignedIn = CheckActiveSession();
            string userEmail = null;
            ApplicationUser user = null;

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user != null)
                {
                    userEmailExists = true;
                    userEmail = user.Email;
                }
            }
            else
            {
                bool userEmailExistsInTempData = TempData.TryGetValue(TempDataKeys.UserEmail, out object userEmailTempData);

                if (userEmailExistsInTempData)
                {
                    userEmail = userEmailTempData.ToString();

                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        userEmailExists = true;
                    }
                }
            }

            if (userEmailExists)
            {
                // retain TempData so page reload keeps user on the same page
                TempData.Keep();

                TOTPAccessRecoveryChallengeInputModel model = await GenerateTOTPAccessRecoveryChallengeInputModel(userEmail, user);

                return GenerateArray(model, null);
            }
            else
            {
                return GenerateArray(null, defaultRoute);
            }
        }


        /// <summary>
        ///     private async Task<TOTPAccessRecoveryChallengeInputModel> GenerateTOTPAccessRecoveryChallengeInputModel(
        ///         string userEmail,
        ///         ApplicationUser challengedUser = null
        ///     )
        ///     
        ///     1. Checks if the user email is null or whitespace. If so, the method returns null. If not, the
        ///         method continues.
        ///     
        ///     2. If the user param is null, fetches the user using the UserManager.FindByEmailAsync() method.
        ///         In case the user is not found, the method returns null.
        ///         
        ///     3. If the user is found, and the user has a confirmed email but did not complete
        ///         account registration, an email is sent to the user to request to complete account
        ///             registration. Then the method returns null.
        ///             
        ///     4. If the user has a confirmed email and the user did complete account registration,
        ///         an input model containing the user's email is created and the method returns
        ///             this input model.
        ///             
        ///     5. For all other scenarios, the method returns null.
        /// </summary>
        /// <param name="userEmail">The user's email supplied by the calling function</param>
        /// <param name="challengedUser">
        ///     The ApplicationUser object, optionally provided by the calling function
        /// </param>
        /// <returns>
        ///     The created input model (TOTPAccessRecoveryChallengeInputModel)
        ///         or,
        ///             null if creation of the input model is not possible.
        /// </returns>
        private async Task<TOTPAccessRecoveryChallengeInputModel> GenerateTOTPAccessRecoveryChallengeInputModel(
            string userEmail,
            ApplicationUser challengedUser = null
        ) {
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                ApplicationUser user = challengedUser ?? await UserManager.FindByEmailAsync(userEmail);

                if (user == null)
                {
                    // user doesn't exist, return null to redirect to another page
                    return null;
                }
                else if (user.EmailConfirmed && !user.AccountRegistered)
                {
                    // user exists with confirmed email and unregistered account, send email to complete registration
                    await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, userEmail, user.UserName);

                    return null;
                }
                else if (user.EmailConfirmed && user.AccountRegistered)
                {
                    // user exists with registered account and confirmed email, so user is trying to reset TOTP access
                    return new TOTPAccessRecoveryChallengeInputModel
                    {
                        Email = userEmail
                    };
                }
            }

            return null;
        } 


        /// <summary>
        ///     public async Task<object[]> ManageEmailChallenge(string defaultRoute, string returnUrl = null)
        ///     
        ///     Handles the creation of the view model or redirect route for the EmailChallenge GET action.
        ///     
        ///     1. Checks if the current user is signed in. If signed in, the user is fetched by calling the
        ///         UserManager.GetUserAsync() method. If not, the user email is derived from the TempData.
        ///     
        ///     2. If the user or user's email was not found, the defaultRoute param is returned in the result array.
        ///         If the user email was found, the method continues.
        ///     
        ///     3. The email record id for a resend email is also derived from the TempData.
        ///     
        ///     4. Once the required information is gathered, the view/input model is constructed by passing the information to
        ///         the GenerateEmailChallengeInputModel() method.
        ///         
        ///     5. Finally the view/input model is returned in the result array.
        /// </summary>
        /// <param name="defaultRoute">
        ///     The route to redirect the page to, when a view model is not returned.
        /// </param>
        /// <param name="returnUrl">
        ///     The return url to redirect the page to, when user has successfully completed the email challenge.
        /// </param>
        /// <returns>
        ///     An array of objects, containing the view/input model for the Email Challenge page and null
        ///         or
        ///             null and a redirect route.
        /// </returns>
        public async Task<object[]> ManageEmailChallenge(string defaultRoute, string returnUrl = null)
        {
            string userEmail = null;
            bool userEmailExists = false;
            ApplicationUser user = null;

            // check if user is signed in
            bool currentUserSignedIn = CheckActiveSession();

            if (currentUserSignedIn)
            {
                user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

                if (user != null)
                {
                    userEmailExists = true;
                    userEmail = user.Email;
                }
            }
            else
            {
                bool userEmailExistsInTempData = TempData.TryGetValue(TempDataKeys.UserEmail, out object userEmailTempData);

                if (userEmailExistsInTempData)
                {
                    userEmail = userEmailTempData.ToString();
                    
                    if (string.IsNullOrWhiteSpace(userEmail))
                    {
                        userEmail = null;
                        userEmailExists = false;
                    }
                    else
                    {
                        userEmailExists = true;
                    }
                }
            }

            if (!userEmailExists)
            {
                return GenerateArray(null, defaultRoute);
            }
            else
            {
                // retain TempData as user isn't signed in, so page reload keeps user on the same page
                if (!currentUserSignedIn)
                    TempData.Keep();

                bool resendEmailRecordIdExists = TempData.TryGetValue(
                    TempDataKeys.ResendEmailRecordId,
                    out object resendEmailRecordIdTempData
                );

                string resendEmailRecordId = resendEmailRecordIdExists ? resendEmailRecordIdTempData.ToString() : null;

                EmailChallengeInputModel model = await GenerateEmailChallengeInputModel(userEmail, resendEmailRecordId, returnUrl, user);

                return GenerateArray(model, null);
            }
        }


        /// <summary>
        ///     private async Task<EmailChallengeInputModel> GenerateEmailChallengeInputModel(
        ///         string userEmail,
        ///         string resendEmailRecordId,
        ///         string returnUrl,
        ///         ApplicationUser challengedUser = null
        ///     )
        ///         
        ///     Generates the EmailChallengeInputModel object.
        ///     
        ///     1. Checks if the user exists. If it doesn't exist, returns null.
        /// 
        ///     2. Checks if the user completed the account registration process after they confirmed their email.
        ///         If they did not, an email is sent to them to complete registration. And the method returns null.
        ///     
        ///     3. Checks if (i) the user has registered their account and confirmed their email address,
        ///         or if (ii) user has unregistered account and unconfirmed email.
        ///             In case (i), the user is signing in and requires an email challenge as a security measure.
        ///                 In case (ii), the user is registering their account and requires an email challenge to
        ///                     verify their email address.
        ///                         When either case is true, the view/input model is created and returned.
        ///                         
        ///     4. For any other conditions, null is returned.
        /// </summary>
        /// <param name="userEmail">User's email address</param>
        /// <param name="resendEmailRecordId">The id for the email record to resend the challenge email</param>
        /// <param name="returnUrl">The url to return to when the user successfully completes the challenge</param>
        /// <param name="challengedUser">
        ///     The ApplicationUser object sent from the calling function. This can be null if the user's email is
        ///         derived from TempData which happens when the user is signing up.
        /// </param>
        /// <returns>The email challenge view/input model object or null</returns>
        private async Task<EmailChallengeInputModel> GenerateEmailChallengeInputModel(string userEmail, string resendEmailRecordId, string returnUrl, ApplicationUser challengedUser = null)
        {
            ApplicationUser user = challengedUser ?? await UserManager.FindByEmailAsync(userEmail);

            if (user == null)
            {
                // user doesn't exist
                return null;
            }
            else if (user.EmailConfirmed && !user.AccountRegistered)
            {
                // user exists with confirmed email and unregistered account, send email to complete registration
                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, userEmail, user.UserName);

                return null;
            }
            else if ((!user.EmailConfirmed && !user.AccountRegistered) || (user.EmailConfirmed && user.AccountRegistered))
            {
                // user exists with unregistered account and unconfirmed email, so user is signing up
                // or
                // user exists with registered account and confirmed email, so user is signing in

                EmailChallengeInputModel model = new EmailChallengeInputModel
                {
                    Email = userEmail,
                    ResendEmailRecordId = resendEmailRecordId,
                    ReturnUrl = returnUrl
                };

                return model;
            }

            return null;
        }


        /// <summary>
        ///     public async Task<string> ManageTOTPChallengeSuccess(
        ///         ApplicationUser user,
        ///         string resendEmailRecordId,
        ///         string context,
        ///         string targetRoute
        ///     )
        ///     
        ///     Manages the application flow after a successful TOTP challenge.
        /// 
        ///     1. The flow is controlled using defined contexts based on the user's action.
        ///     
        ///     2. These contexts are (i) UserActionContexts.ConfirmEmailChallenge - confirm email challenge is successful,
        ///         (ii) UserActionContexts.SignInTOTPChallenge - sign in TOTP challenge is successful,
        ///             (iii) UserActionContexts.SignInEmailChallenge - sign in email challenge is successful,
        ///                 (iv) UserActionContexts.TOTPAccessRecoveryChallenge - TOTP access recovery challenge is successful,
        ///                     (v) UserActionContexts.TOTPChallenge - dedicated TOTP challenge is successful,
        ///                         (vi) The default context means there is a mismatch of supplied vs available contexts
        ///                             and a generic error message for the user is added..
        ///                         
        ///     3. A unique method is called to handle the flow for each context.
        ///     
        ///     4. These methods all return an appropriate route to redirect the application.
        ///     
        ///     5. Before the start of the flow control, any email record that is no longer in use is deleted.
        /// </summary>
        /// <param name="user">ApplicationUser object</param>
        /// <param name="resendEmailRecordId">The id for the email record to be deleted</param>
        /// <param name="context">The context relating to which the flow is to be handled</param>
        /// <param name="targetRoute">A route which acts as a return url for when the challenge is successfully completed</param>
        /// <returns>The route to redirect the application to</returns>
        public async Task<string> ManageTOTPChallengeSuccess(
            ApplicationUser user,
            string resendEmailRecordId,
            string context,
            string targetRoute
        ) {
            string redirectRoute = null;

            // delete an email record as it has served its purpose
            if (!string.IsNullOrWhiteSpace(resendEmailRecordId))
                await EmailService.DeleteEmailRecord(resendEmailRecordId, user);

            switch (context)
            {
                case UserActionContexts.ConfirmEmailChallenge:
                    redirectRoute = await ConfirmUserEmail(user);
                    break;
                case UserActionContexts.SignInTOTPChallenge:
                    redirectRoute = await SignInTOTP(user);
                    break;
                case UserActionContexts.SignInEmailChallenge:
                    redirectRoute = await SignIn(user);
                    break;
                case UserActionContexts.TOTPAccessRecoveryChallenge:
                    redirectRoute = await AcknowledgeResetTOTPAccessRequest(user);
                    break;
                case UserActionContexts.TOTPChallenge:
                    redirectRoute = await AddTOTPAuthorizationClaim(user, targetRoute);
                    break;
                default:
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");
                    break;
            }

            return redirectRoute;
        }


        /// <summary>
        ///     private async Task<string> ConfirmUserEmail(ApplicationUser user)
        ///     
        ///     Confirms the user's email address.
        ///     
        ///     1. Sets the EmailConfirmed property of the user to true.
        ///     
        ///     2. Updates the user so the change is persisted in the database using the
        ///         UserManager.UpdateAsync() method.
        ///         
        ///     3. If the update fails, all errors are added to the ModelState for the
        ///         user and the method returns null.
        ///         
        ///     4. If the update succeeds, an email is sent to the user notifying them
        ///         about the successful confirmation of their email address.
        ///         
        ///        The user's email address is stored in TempData with an expiry DateTime.
        ///        
        ///        Then the method returns a redirect route to the Register TOTP Access
        ///         page.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>The route to redirect the application or null</returns>
        private async Task<string> ConfirmUserEmail(ApplicationUser user)
        {
            user.EmailConfirmed = true;

            IdentityResult updateUser = await UserManager.UpdateAsync(user);

            if (updateUser.Succeeded)
            {
                Console.WriteLine($"Error updating user");

                // add errors to ModelState
                foreach (IdentityError error in updateUser.Errors)
                    ActionContext.ModelState.AddModelError(string.Empty, error.Description);

                return null;
            }
            else
            {
                // temporarily disabling this email
                // await EmailService.SendEmailConfirmedEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                TempData[TempDataKeys.UserEmail] = user.Email;
                SetTempDataExpiryDateTime();

                string redirectRoute = GenerateRouteUrl("RegisterTOTPAccess", "SignUp", "Enroll");

                return redirectRoute;
            }
        }


        /// <summary>
        ///     private async Task<string> SignInTOTP(ApplicationUser user)
        ///     
        ///     Signs in the user after the user provided valid initial sign in credentials.
        ///     
        ///     1. Checks if two factor authentication is enabled for the user by calling the
        ///         UserManager.GetTwoFactorEnabledAsync() method. If 2FA is not enabled, the user is
        ///             signed in immediately using the SignIn() method and a redirect route is returned.
        ///                 If 2FA is enabled, the method continues.
        ///                 
        ///     2. All previous unsuccessful sign in attempts are reset by calling the ResetSignInAttempts() method.
        ///     
        ///     3. An email verification code is generated using the UserManager.GenerateTwoFactorTokenAsync() method.
        ///         A custom token provider (CustomTokenOptions.GenericTOTPTokenProvider) is used to generate this
        ///             verification code.
        ///             
        ///     4. An email is sent to the user with the verification code generated above inside it. A resend email
        ///         record id is obtained during this process.
        ///         
        ///     5. The user's email address and the resend email record id is stored in the TempData so they persist
        ///         when the user is redirected to another page.
        ///         
        ///     6. Finally, a redirect route to the Email Challenge page is returned. The user will be redirected to
        ///         this page by the SignIn GET action.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>The route to redirect the user</returns>
        private async Task<string> SignInTOTP(ApplicationUser user)
        {
            bool twoFactorAuthenticationEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

            if (!twoFactorAuthenticationEnabled)
            {
                string redirectRoute = await SignIn(user);

                return redirectRoute;
            }

            await ResetSignInAttempts(user);

            string emailVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(user, CustomTokenOptions.GenericTOTPTokenProvider);

            string resendEmailRecordId = await EmailService.SendNewSessionVerificationEmail(
                AutomatedEmails.NoReply,
                user.Email,
                user.UserName,
                emailVerificationCode
            );

            TempData[TempDataKeys.UserEmail] = user.Email;
            TempData[TempDataKeys.ResendEmailRecordId] = resendEmailRecordId.ToString();

            return GenerateRouteUrl("EmailChallenge", "Authentication", "Access");
        }


        /// <summary>
        ///     public async Task<string> SignIn(ApplicationUser user)
        /// 
        ///     Signs in the user.
        ///     
        ///     1. Resets all previous failed sign in attempts.
        ///     
        ///     2. Updates the security stamp of the user. If the update fails for some reason, errors are
        ///         printed to the application console for debugging. And an error message is added to the
        ///             ModelState for the user to see. Then the method returns null.
        ///             
        ///     3. If the update is successful, the method continues.
        ///     
        ///     4. Creates an amr (Authentication Methods Reference) claim with the value "mfa" (ProjectClaimValues.AMRTypeMFA).
        ///         Then the user is signed in using the SignInManager.SignInWithClaimsAsync() method which also
        ///             sets the amr claim to override its default value of "pwd" set by Duende Identity Server.
        ///         
        ///     4. Updates the last signed in time stamp for the user. If the update fails, the user
        ///         is signed out for security and any errors are printed to the console. An error message is also
        ///             added to the ModelState. Then this method returns null.
        ///     
        ///     5. If the update is successful, an email is sent to the user notifying them about their new session.
        ///         All TempData are cleared and a route is returned to which the user will be redirected to.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>
        ///     The route to redirect the application in case of a successful sign in
        ///         or
        ///             null if signing in failed.
        /// </returns>
        public async Task<string> SignIn(ApplicationUser user)
        {
            await ResetSignInAttempts(user);

            // update security stamp of the user so other active sessions are logged out on the next request
            IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(user);

            if (!updateSecurityStamp.Succeeded)
            {
                Console.WriteLine($"Error updating security stamp");

                foreach (IdentityError error in updateSecurityStamp.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
            
                return null;
            }
            else
            {
                // setting amr (Authentication Methods Reference) claim with value "mfa"
                // Duende Identity Server will set it to "pwd" by default if we don't set it
                Claim AMRClaim = CreateMFATypeAMRClaim();
                IList<Claim> additionalClaims = new List<Claim> { AMRClaim };

                Console.WriteLine("Overriding 'amr' claim value: 'mfa'");
                await SignInManager.SignInWithClaimsAsync(user, false, additionalClaims);

                user.UpdateLastSignedInTimeStamp();

                // record sign in timestamps
                IdentityResult updateUser = await UserManager.UpdateAsync(user);

                if (!updateUser.Succeeded)
                {
                    Console.WriteLine("Error updating user");

                    foreach (IdentityError error in updateUser.Errors)
                        Console.WriteLine(error.Description);
                    
                    ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");

                    // could not update user information, so signout user and redirect to sign in page
                    await SignOut();

                    return null;
                }
                else
                {
                    // send email to user about new session
                    // temporarily disabling this email
                    // await EmailService.SendNewActiveSessionNotificationEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                    // clear all temp data which is unnecessary at this point
                    TempData.Clear();

                    return GenerateRouteUrl("Dashboard", "Pages", "General");
                }
            }
        }


        /// <summary>
        ///     private async Task<string> AcknowledgeResetTOTPAccessRequest(ApplicationUser user)
        ///     
        ///     Acknowledges the user's need to reset TOTP authenticator.
        /// 
        ///     1. Updates the user's RequiresAuthenticatorReset property to true. If the update
        ///         fails, all errors are printed to the console. And an error message is added to the
        ///             ModelState for the user. Then the method returns null.
        ///             
        ///     2. In case the update succeeded, the user is signed out by calling the SignOut() method.
        ///     
        ///     3. The user's email is set in the TempData so it persists after a page redirect. An
        ///         expiry DateTime is also set in the TempData using the SetTempDataExpiryDateTime() method.
        ///             Any attempt to register a new TOTP authenticator after the expiry DateTime has
        ///                 passed will be blocked.
        ///     
        ///     4. Finally, the method returns a redirect route to the Register TOTP Access page.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>
        ///     The route to redirect the application upon successful acknowledgement of
        ///         user's authenticator reset status
        ///             or,
        ///                 null if acknowledgement fails.
        /// </returns>
        private async Task<string> AcknowledgeResetTOTPAccessRequest(ApplicationUser user)
        {
            user.RequiresAuthenticatorReset = true;

            IdentityResult updateUser = await UserManager.UpdateAsync(user);

            if (!updateUser.Succeeded)
            {
                Console.WriteLine("Error updating user");

                foreach (IdentityError error in updateUser.Errors)
                    Console.WriteLine(error.Description);
                
                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again with a different recovery code.");

                // reset TOTP access request acknowledgement failed, return null to return ViewModel
                return null;
            }

            // sign out user
            await SignOut();

            TempData[TempDataKeys.UserEmail] = user.Email;
            SetTempDataExpiryDateTime();

            // redirect user to Register TOTP Access page
            return GenerateRouteUrl("RegisterTOTPAccess", "SignUp", "Enroll");
        }


        /// <summary>
        ///     private async Task<string> AddTOTPAuthorizationClaim(ApplicationUser user, string targetRoute)
        ///     
        ///     Adds the TOTP authorization claim for the user.
        ///     
        ///     1. Checks if the user has an expired ProjectClaimTypes.TOTPAuthorizationExpiry claim.
        ///     
        ///     2. Calls the CreateTOTPAutorizationExpiryClaim() method which creates a new Claim object with
        ///         ProjectClaimTypes.TOTPAuthorizationExpiry as type and a stringified version of a DateTime
        ///             object as value. The DateTime object is created by that method by adding
        ///                 AccountOptions.TOTPAuthorizationDurationInSeconds seconds to the current UTC time.
        ///             
        ///     3. If an expired claim was found in step 1, all expired claims with the matching
        ///         type (ProjectClaimTypes.TOTPAuthorizationExpiry) is listed and then deleted by calling
        ///             the UserManager.RemoveClaimsAsync() method. If no expired claim was found, proceed to
        ///                 step 6.
        ///         
        ///     4. If the deletion fails, errors are printed to the console and an error message is added
        ///         to the ModelState. Then this method returns null.
        ///         
        ///     5. If deletion succeeded, the method continues.
        ///     
        ///     6. The newly created claim in step 2 is added to the user's current claims using the
        ///         UserManager.AddClaimAsync() method. If adding the claim failed, errors are printed to
        ///             the console. An error message is added to the ModelState for the user to see. Then
        ///                 this method returns null.
        ///                 
        ///     7. If adding the claim succeeded, the user's current session is refreshed by calling the method
        ///         SignInManager.RefreshSignInAsync().
        ///         
        ///     8. Finally, this method returns the targetRoute param.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <param name="targetRoute">
        ///     The route to redirect the user upon successful addition of the
        ///         TOTP Authorization claim (ProjectClaimTypes.TOTPAuthorizationExpiry).
        /// </param>
        /// <returns>
        ///     The targetRoute param upon successful addition of the claim
        ///         or,
        ///             null if the addition failed.
        /// </returns>
        private async Task<string> AddTOTPAuthorizationClaim(ApplicationUser user, string targetRoute)
        {            
            bool userHasExpiredClaim = ActionContext.HttpContext.User.HasClaim(
                claim => claim.Type == ProjectClaimTypes.TOTPAuthorizationExpiry
            );
            
            Claim newClaim = CreateTOTPAutorizationExpiryClaim();

            if (userHasExpiredClaim)
            {
                IEnumerable<Claim> expiredClaims = ActionContext.HttpContext.User.FindAll(
                    claim => claim.Type == ProjectClaimTypes.TOTPAuthorizationExpiry
                );

                IdentityResult deleteExpiredClaims = await UserManager.RemoveClaimsAsync(user, expiredClaims);

                if (!deleteExpiredClaims.Succeeded)
                {
                    Console.WriteLine("Error deleting expired user claims");

                    // log errors
                    foreach (IdentityError error in deleteExpiredClaims.Errors)
                        Console.WriteLine(error.Description);

                    ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                    return null;
                }
            }

            IdentityResult addNewUserClaim = await UserManager.AddClaimAsync(user, newClaim);

            if (!addNewUserClaim.Succeeded)
            {
                Console.WriteLine($"Error adding user claim of type {ProjectClaimTypes.TOTPAuthorizationExpiry}");

                // log errors
                foreach (IdentityError error in addNewUserClaim.Errors)
                    Console.WriteLine(error.Description);

                ActionContext.ModelState.AddModelError(string.Empty, "Something went wrong. Please try again later.");

                return null;
            }

            // refreshing user sign in so the claim updates take effect immediately
            await SignInManager.RefreshSignInAsync(user);

            return targetRoute;
        }


        /// <summary>
        ///     public async Task SignOut()
        ///     
        ///     Signs out the current user.
        ///     
        ///     1. Retrieves the current user by calling the UserManager.GetUserAsync() method.
        ///     
        ///     2. If the user is found, the user's security stamp is updated using the
        ///         UserManager.UpdateSecurityStampAsync() method. In case there were any errors while
        ///             updating, the errors are printed out to the console.
        ///         
        ///     3. Any TempData is cleared for security purposes.
        ///     
        ///     4. Then the SignInManager.SignOutAsync() method is called to clear the authentication cookie.
        /// </summary>
        /// <returns>void</returns>
        public async Task SignOut()
        {
            ApplicationUser currentUser = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (currentUser != null)
            {
                // the authentication cookie can be reset manually. So if its compromised,
                // then someone can bypass authentication when using the application
                // so the session needs to be explicitly invalidated by updating the security stamp
                IdentityResult updateSecurityStamp = await UserManager.UpdateSecurityStampAsync(currentUser);

                if (!updateSecurityStamp.Succeeded)
                {
                    Console.WriteLine("Error updating security stamp during user signout");

                    foreach (IdentityError error in updateSecurityStamp.Errors)
                        Console.WriteLine(error.Description);
                }
            }

            // delete all TempData
            TempData.Clear();

            // but delete authentication cookie anyways
            await SignInManager.SignOutAsync();
        }


        /// <summary>
        ///     public async Task RefreshUserSignIn(ApplicationUser user)
        ///     
        ///     Refreshes the user's current session.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>void</returns>
        public async Task RefreshUserSignIn(ApplicationUser user)
        {
            // delete all TempData
            TempData.Clear();

            await SignInManager.RefreshSignInAsync(user);
        }


        /// <summary>
        ///     public async Task<List<string>> GenerateTOTPRecoveryCodes(
        ///         ApplicationUser user, int numberOfRecoveryCodes
        ///     )
        ///     
        ///     Generates TOTP access recovery codes for the user.
        ///     
        ///     1. Using a while loop, the method keeps creating TOTP access recovery
        ///         codes for the user until an attempt creates the number of desired
        ///             recovery codes. This is because the method used to create these
        ///                 codes - UserManager.GenerateNewTwoFactorRecoveryCodesAsync()
        ///                     removes any duplicate codes created per attempt.
        ///                     
        ///        Since we need at least "numberOfRecoveryCodes" different codes, we have
        ///         to use the while loop.
        ///         
        ///        Finally, the method returns the created recovery codes.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <param name="numberOfRecoveryCodes">The number of recovery codes to create</param>
        /// <returns>A list of strings containing the TOTP access recovery codes</returns>
        public async Task<List<string>> GenerateTOTPRecoveryCodes(ApplicationUser user, int numberOfRecoveryCodes)
        {
            List<string> recoveryCodes = new List<string>();

            while (recoveryCodes.Count < numberOfRecoveryCodes)
            {
                recoveryCodes = (await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, numberOfRecoveryCodes)).ToList();
            }

            return recoveryCodes;
        }


        /// <summary>
        ///     public async Task RecordUnsuccessfulSignInAttempt(ApplicationUser user)
        ///     
        ///     Records an unsuccessful sign in attempt by the user.
        ///     
        ///     1. Saves an unsuccessful sign in attempt by calling the method UserManager.AccessFailedAsync().
        ///     
        ///     2. If the save fails, any errors are printed in the console.
        ///     
        ///     3. If the save succeeded, a check is performed to determine if the user is locked out. In case
        ///         the user was locked out, an email is sent to the user with instruction for next steps.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>void</returns>
        public async Task RecordUnsuccessfulSignInAttempt(ApplicationUser user)
        {
            IdentityResult saveUnsuccessfulAttempt = await UserManager.AccessFailedAsync(user);

            if (!saveUnsuccessfulAttempt.Succeeded)
            {
                Console.WriteLine($"Could not record unsuccessful SignIn attempt.");

                foreach (IdentityError error in saveUnsuccessfulAttempt.Errors)
                    Console.WriteLine(error.Description);
            }

            // if increasing the failed count results in account lockout, notify the user
            bool isUserLockedOut = await UserManager.IsLockedOutAsync(user);

            if (isUserLockedOut)
                await EmailService.SendAccountLockedOutEmail(AutomatedEmails.NoReply, user.Email, user.UserName);
        }


        /// <summary>
        ///     private Claim CreateMFATypeAMRClaim()
        ///     
        ///     Creates a new claim of the type JwtClaimTypes.AuthenticationMethod
        ///         and value ProjectClaimValues.AMRTypeMFA.
        /// </summary>
        /// <returns>The created claim</returns>
        private Claim CreateMFATypeAMRClaim()
        {
            return new Claim(JwtClaimTypes.AuthenticationMethod, ProjectClaimValues.AMRTypeMFA);
        }


        /// <summary>
        ///     private Claim CreateTOTPAutorizationExpiryClaim()
        ///     
        ///     Creates a TOTP authorization expiry claim.
        ///     
        ///     1. Creates a DateTime object that is AccountOptions.TOTPAuthorizationDurationInSeconds seconds
        ///         more from the current UTC time.
        ///         
        ///     2. Creates a new Claim object with ProjectClaimTypes.TOTPAuthorizationExpiry as type and
        ///         stringified value of the DateTime object created above as value.
        ///         
        ///     3. Returns the newly created Claim object.
        /// </summary>
        /// <returns>
        ///     A Claim object with ProjectClaimTypes.TOTPAuthorizationExpiry as its type and the stringified
        ///         DateTime object as its value.
        /// </returns>
        private Claim CreateTOTPAutorizationExpiryClaim()
        {
            DateTime authorizationExpiryDateTime = DateTime.UtcNow.AddSeconds(AccountOptions.TOTPAuthorizationDurationInSeconds);

            return new Claim(ProjectClaimTypes.TOTPAuthorizationExpiry, authorizationExpiryDateTime.ToString());
        }


        /// <summary>
        ///     private async Task ResetSignInAttempts(ApplicationUser user)
        ///     
        ///     Resets the counter for previous sign in attempts by the user.
        ///     
        ///     1. The counter is reset by calling the UserManager.ResetAccessFailedCountAsync() method.
        ///     
        ///     2. If the reset is unsuccessful, any errors are printed to the console.     
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>void</returns>
        private async Task ResetSignInAttempts(ApplicationUser user)
        {
            IdentityResult resetAttempts = await UserManager.ResetAccessFailedCountAsync(user);

            if (!resetAttempts.Succeeded)
            {
                Console.WriteLine($"Cound not record successful SignIn attempt.");

                foreach (IdentityError error in resetAttempts.Errors)
                    Console.WriteLine(error.Description);
            }
        }


        /// <summary>
        ///     public async Task<bool> VerifySignInPrerequisites(ApplicationUser user)
        ///     
        ///     Verifies prerequiste factors to determine if the user can sign in.
        ///     
        ///     1. Checks if the user exists with the given email.
        ///     
        ///     2. Checks if the user account is in archived status.
        ///     
        ///     3. Checks if the user account is in blocked status.
        ///     
        ///     4. Checks if the user's email is confirmed.
        ///     
        ///     5. Checks if the user has finished their account registration process. If not,
        ///         sends an email to inform the user to complete their registration.
        ///     
        ///     6. Checks if the user requires an authenticator reset. If true, an email is sent to the user to
        ///         reset their authenticator.
        ///     
        ///     7. Checks if the user can not sign in or if the user is locked out.
        ///     
        ///     8. If any of the above checks get an unsatisfactory result, the method returns false.
        ///         Otherwise, the method returns true.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>True or false indicating if the user can sign in</returns>
        public async Task<bool> VerifySignInPrerequisites(ApplicationUser user)
        {
            // check if user exists with the given email
            if (user == null)
                return false;

            if (user.Archived)
                return false;

            if (user.Blocked)
            {
                Console.WriteLine($"User with id {user.Id} is blocked.");
                return false;
            }
            
            if (!user.EmailConfirmed)
            {
                Console.WriteLine($"User with id {user.Id} has unconfirmed email.");
                return false;
            }

            // if user exists but did not complete registration, send email to complete registration
            if (!user.AccountRegistered)
            {
                Console.WriteLine($"User with id {user.Id} has unregistered account.");

                await EmailService.SendAccountNotRegisteredEmail(AutomatedEmails.NoReply, user.Email, user.UserName);

                return false;
            }

            // if user exists but requires authenticator reset, send email to reset
            if (user.AccountRegistered && user.RequiresAuthenticatorReset)
            {
                Console.WriteLine($"User with id {user.Id} needs to reset TOTP access authenticator.");

                await EmailService.SendResetTOTPAccessReminderEmail(AutomatedEmails.NoReply, user.Email, user.Email);

                return false;
            }

            // check if user is allowed to sign in && user is not locked out            
            bool canUserSignIn = await SignInManager.CanSignInAsync(user);
            bool appSupportsLockout = UserManager.SupportsUserLockout;
            bool isUserLockedOut = await UserManager.IsLockedOutAsync(user);

            if (!canUserSignIn || (appSupportsLockout && isUserLockedOut))
            {
                Console.WriteLine($"User with id {user.Id} cannot signin or is locked out.");

                return false;
            }

            return true;
        }


        /// <summary>
        ///     public bool CheckActiveSession()
        ///     
        ///     Checks if the current user is signed in.
        /// </summary>
        /// <returns>boolean</returns>
        public bool CheckActiveSession()
        {
            return SignInManager.IsSignedIn(ActionContext.HttpContext.User);
        }


        /// <summary>
        ///     public async Task<bool> VerifyTOTPCode(ApplicationUser user, string tokenProvider, string verificationCode)
        ///     
        ///     Verifies the TOTP code of the user for a particular token provider.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <param name="tokenProvider">The token provider to verify the TOTP code</param>
        /// <param name="verificationCode">The TOTP code</param>
        /// <returns>The verification result as true or false</returns>
        public async Task<bool> VerifyTOTPCode(ApplicationUser user, string tokenProvider, string verificationCode)
        {
            bool verificationResult = await UserManager.VerifyTwoFactorTokenAsync(
                user,
                tokenProvider,
                verificationCode
            );

            return verificationResult;
        }


        /// <summary>
        ///     public async Task<bool> VerifyTOTPAccessRecoveryCode(ApplicationUser user, string verificationCode)
        ///     
        ///     Verifies the TOTP access recovery code.
        ///     
        ///     1. Verifies the user submitted recovery code using the UserManager.RedeemTwoFactorRecoveryCodeAsync()
        ///         method.
        ///         
        ///     2. If the verification fails, all errors are printed to the console and the method returns false.
        ///     
        ///     3. If the verification succeeds, the method returns true.
        /// </summary>
        /// <param name="user">The ApplicationUser object</param>
        /// <param name="verificationCode">The user submitted TOTP access recovery code</param>
        /// <returns>The TOTP access recovery code verification result as a boolean</returns>
        public async Task<bool> VerifyTOTPAccessRecoveryCode(ApplicationUser user, string verificationCode)
        {
            // verify TOTP access recovery code
            IdentityResult redeemTOTPAccessRecoveryCode = await UserManager.RedeemTwoFactorRecoveryCodeAsync(user, verificationCode);
        
            if (!redeemTOTPAccessRecoveryCode.Succeeded)
            {
                Console.WriteLine("Error redeeming user TOTP access recovery code");

                foreach (IdentityError error in redeemTOTPAccessRecoveryCode.Errors)
                    Console.WriteLine(error.Description);

                return false;
            }

            return true;
        }


        /// <summary>
        ///     public string GenerateQRCodeUri(string email, string authenticatorKey)
        ///     
        ///     Generates the QR code uri for the user's authenticator key.
        /// </summary>
        /// <param name="email">An email address for the user</param>
        /// <param name="authenticatorKey">The authenticator key of the user</param>
        /// <returns>A stringified QR code uri for the user's authenticator key</returns>
        public string GenerateQRCodeUri(string email, string authenticatorKey)
        {
            const string AuthenticatorKeyUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

            return string.Format(
                AuthenticatorKeyUriFormat,
                UrlEncoder.Encode("Bonic"),
                UrlEncoder.Encode(email),
                authenticatorKey
            );
        }


        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            ResourcesDisposed = true;
        }
    }
}
