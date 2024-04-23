using System;
using System.Threading.Tasks;
using System.Web;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Areas.Access.Models.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Constants.Account;
using CoreIdentityServer.Internals.Constants.Storage;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using CoreIdentityServer.Internals.Authorization.Handlers;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class AuthenticationService : BaseService, IDisposable
    {
        private readonly UserManager<ApplicationUser> UserManager;
        private IdentityService IdentityService;
        private readonly IIdentityServerInteractionService InteractionService;
        private ActionContext ActionContext;
        private readonly ITempDataDictionary TempData;
        private readonly RouteEndpointService RouteEndpointService;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            IdentityService identityService,
            IIdentityServerInteractionService interactionService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            RouteEndpointService routeEndpointService
        ) {
            UserManager = userManager;
            IdentityService = identityService;
            InteractionService = interactionService;
            ActionContext = actionContextAccessor.ActionContext;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            RouteEndpointService = routeEndpointService;
            RootRoute = GenerateRouteUrl("SignIn", "Authentication", "Access");
        }

        /// <summary>
        ///     public string ManageAccessDenied(string returnUrl)    
        /// 
        ///     Manages the AccessDenied GET action.
        ///     
        ///     1. Checks if the return url is available. If not, null is returned.
        ///     
        ///     2. If available, checks if the return url is one of the urls that require a TOTP Challenge.
        ///     
        ///     3. And checks if the user is authorized according to the TOTPChallengeHandler.IsUserAuthorized() method.
        ///     
        ///     4. If the TOTP challenge is necessary, because the return url requires a TOTP challenge and the user doesn't
        ///         have TOTP authorization, a redirect route to the TOTP Challenge page is generated and returned from the
        ///         function with the initial return url in encoded form added in the redirect route as a query string.
        ///         
        ///     5. If the TOTP challenge is not necessary, the method returns null.
        /// </summary>
        /// <param name="returnUrl">
        ///     The url a user was trying to visit when the TOTPChallenge authorization filter restricted the user and 
        ///         sent them to the Access Denied page.
        /// </param>
        /// <returns>
        ///     Either the TOTP Challenge page route with the return url encoded and added to it as a query string,
        ///         or,
        ///             null.
        /// </returns>
        public string ManageAccessDenied(string returnUrl)
        {
            bool returnUrlAvailable = !string.IsNullOrWhiteSpace(returnUrl);

            if (returnUrlAvailable)
            {
                bool returnUrlRequiresTOTPChallenge = RouteEndpointService.EndpointRoutesRequiringTOTPChallenge.Contains(returnUrl.ToLower());
                bool userHasTOTPAuthorization = returnUrlRequiresTOTPChallenge ? TOTPChallengeHandler.IsUserAuthorized(ActionContext.HttpContext.User) : false;

                if (returnUrlRequiresTOTPChallenge && !userHasTOTPAuthorization)
                {
                    string encodedReturnUrl = HttpUtility.UrlEncode(returnUrl.ToLower());

                    string totpChallengeRedirectRoute = GenerateRouteUrl("totpchallenge", "authentication", "access", $"returnurl={encodedReturnUrl}");

                    return totpChallengeRedirectRoute;
                }
            }

            return null;
        }

        /// <summary>
        ///     public async Task<object[]> ManageEmailChallenge(string returnUrl)
        ///     
        ///     Manages the EmailChallenge GET action.
        ///     
        ///     1. Checks if the returnUrl parameter is valid.
        ///     
        ///     2. Delegates this method's task to the IdentityService.ManageEmailChallenge() method which returns
        ///         an object array containing the view model for the Email Challenge page or a redirect route.
        ///             Note, a default route (RootRoute) is also passed to the method.
        ///         
        ///     3. Returns the object array.
        /// </summary>
        /// <param name="returnUrl">The url to return to, once the user has successfully completed the email challenge.</param>
        /// <returns>
        ///     An array of objects containing the view model for the Email Challenge page or a redirect route, not both.
        /// </returns>
        public async Task<object[]> ManageEmailChallenge(string returnUrl)
        {
            string emailChallengeReturnUrl = IsValidReturnUrl(returnUrl, InteractionService, RouteEndpointService.EndpointRoutes) ? returnUrl : null;

            object[] result = await IdentityService.ManageEmailChallenge(RootRoute, emailChallengeReturnUrl);

            return result;
        }

        /// <summary>
        ///     public async Task<string> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
        ///     
        ///     Manages the EmailChallenge POST action.
        ///     
        ///     1. Checks if the model state has any validation errors. If there are errors, the method returns null.
        ///     
        ///     2. The user is retrieved from the database using the email property of the input model.
        ///     
        ///     3. If the user was not found, a generic error message is added to the model state
        ///         and the method returns null.
        ///     
        ///     4. Otherwise, the IdentityService.VerifySignInPrerequisites() method checks if the user is restricted from
        ///         signing in. If the user is restricted, a generic error message is added to the model state and the method
        ///             returns null.
        ///         
        ///     5. If the user is not restricted from signing in, the IdentityService.VerifyTOTPCode() method verifies
        ///         the totp code submitted by the user during the email challenge.
        ///         
        ///     6. If the totp code is verified successfully, the user is redirected to the route returned by the
        ///         IdentityService.ManageTOTPChallengeSuccess() method or to a valid return url, if present.
        ///         
        ///     7. If the totp code verification failed, an unsuccessful attempt to log in by the user is recorded by
        ///         the method IdentityService.RecordUnsuccessfulSignInAttempt(). Then an error message is added to the
        ///             model state and null is returned.
        /// </summary>
        /// <param name="inputModel">
        ///     The model containing the user's email, the email challenge verification code, the ResendEmailRecordId
        ///         and the return url.
        /// </param>
        /// <returns>
        ///     The route to redirect upon successful email challenge verification
        ///         or,
        ///             null if the challenge verfication failed.
        /// </returns>
        public async Task<string> ManageEmailChallengeVerification(EmailChallengeInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, but don't reveal to end user
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");

                return null;
            }
            else
            {
                bool userCanSignIn = await IdentityService.VerifySignInPrerequisites(user);

                if (!userCanSignIn)
                {
                    // add generic error and return ViewModel
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");

                    return null;
                }
                else
                {
                    bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                        user,
                        CustomTokenOptions.GenericTOTPTokenProvider,
                        inputModel.VerificationCode
                    );

                    if (totpCodeVerified)
                    {
                        string redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                            user,
                            inputModel.ResendEmailRecordId,
                            UserActionContexts.SignInEmailChallenge,
                            null
                        );

                        // signin succeeded & returnUrl present in query string, redirect to returnUrl
                        if (redirectRoute != null &&
                            IsValidReturnUrl(
                                inputModel.ReturnUrl, InteractionService, RouteEndpointService.EndpointRoutes
                            )
                        )
                        {
                            redirectRoute = $"~{inputModel.ReturnUrl}";
                        }

                        return redirectRoute;
                    }
                    else
                    {
                        await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");

                        return null;
                    }
                }
            }
        }

        /// <summary>
        ///     public SignInInputModel ManageSignIn(string returnUrl)
        /// 
        ///     Manages the SignIn GET action.
        ///     
        ///     1. Checks if the returnUrl parameter is valid.
        ///     
        ///     2. Creates a view model object for the view and returns it.
        /// </summary>
        /// <param name="returnUrl">The url to redirect to, once the user has successfully signed in.</param>
        /// <returns>
        ///     The view model for the Sign In page with the return url set in it.
        /// </returns>
        public SignInInputModel ManageSignIn(string returnUrl)
        {
            string signInReturnUrl = IsValidReturnUrl(returnUrl, InteractionService, RouteEndpointService.EndpointRoutes) ? returnUrl : null;

            SignInInputModel viewModel = new SignInInputModel { ReturnUrl = signInReturnUrl };

            return viewModel;
        }

        /// <summary>
        ///     public async Task<string> SignIn(SignInInputModel inputModel)
        /// 
        ///     Manages the SignIn POST action.
        ///     
        ///     1. Checks the ModelState for validity and if its not valid, the method returns a null redirect route.
        ///     
        ///     2. Otherwise, the user is fetched from the database using the email property of the input model.
        ///     
        ///     3. If the user is not found, a generic ModelState error is added and the method returns null.
        ///     
        ///     4. If the user is found, the sign in pre-requisites of the user is checked using the
        ///         IdentityService.VerifySignInPrerequisites() method to determine if the user can sign in.
        ///
        ///     5. If the user can not sign in, a generic model state error is added and the method returns null.
        ///     
        ///     6. If the user can sign in, the TOTP code submitted by the user is verified using the
        ///         IdentityService.VerifyTOTPCode() method.
        ///         
        ///     7. If the verification fails, an unsuccessful sign in attempt is recorded for the user using the
        ///         IdentityService.RecordUnsuccessfulSignInAttempt() method and a generic model state error is added
        ///             and finally the method returns null.
        ///             
        ///     8. If the verification is successful, an appropriate redirect route is determined by the
        ///         IdentityService.ManageTOTPChallengeSuccess() method. Then the return url in the input model
        ///             is validated and if valid, the return url is added to the redirect route as a query string.
        ///                 And finally, that redirect route is returned.
        /// </summary>
        /// <param name="inputModel">
        ///     The model containing the user's email, the sign in TOTP code and the return url.
        /// </param>
        /// <returns>
        ///     The route to redirect to after a successful sign in or null for an unsuccessful sign in.
        /// </returns>
        public async Task<string> SignIn(SignInInputModel inputModel)
        {
            // check if ModelState is valid, if not return null
            if (!ActionContext.ModelState.IsValid)
                return null;

            ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

            if (user == null)
            {
                // user doesn't exist, but don't reveal to end user
                ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");

                return null;
            }
            else
            {
                bool userCanSignIn = await IdentityService.VerifySignInPrerequisites(user);

                if (!userCanSignIn)
                {
                    // add generic error
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
                    
                    return null;
                }
                else
                {
                    bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                        user,
                        TokenOptions.DefaultAuthenticatorProvider,
                        inputModel.TOTPCode
                    );

                    if (totpCodeVerified)
                    {
                        string redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                            user,
                            null,
                            UserActionContexts.SignInTOTPChallenge,
                            null
                        );

                        string redirectRouteQueryString = null;

                        if (IsValidReturnUrl(inputModel.ReturnUrl, InteractionService, RouteEndpointService.EndpointRoutes))
                            redirectRouteQueryString = $"returnurl={HttpUtility.UrlEncode(inputModel.ReturnUrl)}";

                        // add query string since IdentityService.ManageTOTPChallengeSuccess() method doesn't share this concern
                        redirectRoute = GenerateRouteUrl(redirectRoute, redirectRouteQueryString);

                        return redirectRoute;
                    }
                    else
                    {
                        await IdentityService.RecordUnsuccessfulSignInAttempt(user);

                        ActionContext.ModelState.AddModelError(string.Empty, "Invalid email or TOTP code");
                    
                        return null;
                    }
                }
            }
        }

        /// <summary>
        ///     public async Task<SignOutViewModel> ManageSignOut(string signOutId)
        /// 
        ///     Manages the SignOut GET action.
        ///     
        ///     1. Checks if the user is logged in. If not, the session is ended without showing
        ///         a prompt that asks the user if they would like to sign out for confirmation.
        ///         
        ///     2. If the user is logged in, a logout context is retrieved calling the
        ///         InteractionService.GetLogoutContextAsync() method while supplying it
        ///             with the signOutId param.
        ///     
        ///     3. The prompt may also not be shown because of the logout context telling explicitly
        ///         to not show the logout prompt.
        ///         
        ///     4. Returns the view model with the signOutId and ShowSignOutPrompt properties set.
        /// </summary>
        /// <param name="signoutId">The signout id used to get the logout context from Duende Identity Service</param>
        /// <returns>
        ///     The view model for the Sign Out page.
        /// </returns>
        public async Task<SignOutViewModel> ManageSignOut(string signOutId)
        {
            SignOutViewModel viewModel = new SignOutViewModel { SignOutId = signOutId, ShowSignOutPrompt = AccountOptions.ShowSignOutPrompt };

            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            // user is not logged in, sign out without showing prompt
            if (!currentUserSignedIn)
            {
                viewModel.ShowSignOutPrompt = false;

                return viewModel;
            }

            LogoutRequest logoutContext = await InteractionService.GetLogoutContextAsync(signOutId);

            if (logoutContext != null && !logoutContext.ShowSignoutPrompt)
            {
                viewModel.ShowSignOutPrompt = false;
            }

            return viewModel;
        }

        /// <summary>
        ///     public async Task<string> SignOut(SignOutInputModel inputModel)
        ///     
        ///     Manages the SignOut POST action.
        ///
        ///     1. Checks if a logout context is available. If available, creates a view model for the sign out
        ///         page. If not, the view model remains null.
        ///     
        ///     2. Signs out the user using the IdentityService.SignOut() method.
        ///     
        ///     3. Stores the view model, if created, inside the Temp Data so the Signed Out page can load the data
        ///         once the user is signed out and is redirected to that page.
        ///         
        ///     4. Returns a redirect route for the Signed Out page.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model containing the signout id useful to get the logout context.
        /// </param>
        /// <returns>
        ///     The redirect route for the Signed Out page.
        /// </returns>
        public async Task<string> SignOut(SignOutInputModel inputModel)
        {
            SignedOutViewModel viewModel = null;

            LogoutRequest logoutContext = await InteractionService.GetLogoutContextAsync(inputModel.SignOutId);

            if (logoutContext != null)
            {
                viewModel = new SignedOutViewModel
                {
                    AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                    PostLogoutRedirectUri = logoutContext.PostLogoutRedirectUri ?? GenerateAbsoluteLocalUrl("SignIn", "Authentication", "Access"),
                    ClientName = string.IsNullOrWhiteSpace(logoutContext.ClientName) ? logoutContext.ClientId : logoutContext.ClientName,
                    SignOutIFrameUrl = logoutContext.SignOutIFrameUrl,
                    SignOutId = inputModel.SignOutId
                };
            }

            // sign out user
            await IdentityService.SignOut();

            if (viewModel != null)
                TempData[TempDataKeys.SignedOutViewModel] = JsonConvert.SerializeObject(viewModel);

            return GenerateRouteUrl("SignedOut", "Authentication", "Access");
        }

        /// <summary>
        ///     public SignedOutViewModel ManageSignedOut()
        /// 
        ///     Manages the SignedOut GET action.
        ///     
        ///     1. Checks if there is a tempdata containing information to create the signed out page view model.
        ///     
        ///     2. If the tempdata is found, the view model is constructed from it and returned.
        ///         Otherwise null is returned, which generates a generic signed out page.
        /// </summary>
        /// <returns>
        ///     The view model for the Signed Out page with instructions on what things to show in that page.
        /// </returns>
        public SignedOutViewModel ManageSignedOut()
        {
            bool signedOutViewModelExists = TempData.TryGetValue(
                TempDataKeys.SignedOutViewModel,
                out object signedOutViewModelTempData
            );

            if (signedOutViewModelExists)
            {
                SignedOutViewModel viewModel = JsonConvert.DeserializeObject<SignedOutViewModel>((string)signedOutViewModelTempData);
            
                return viewModel;
            }

            return null;
        }

        /// <summary>
        ///     public TOTPChallengeInputModel ManageTOTPChallenge(string returnUrl)
        /// 
        ///     Manages the TOTPChallenge GET action.
        ///     
        ///     1. Checks if the return url is valid.
        ///     
        ///     2. Returns a view model for the TOTP Challenge page with the return url set if it was valid.
        /// </summary>
        /// <returns>
        ///     The view model for the TOTP Challenge page.
        /// </returns>
        public TOTPChallengeInputModel ManageTOTPChallenge(string returnUrl)
        {
            string TOTPChallengeReturnUrl = IsValidReturnUrl(returnUrl, InteractionService, RouteEndpointService.EndpointRoutes) ? returnUrl : null;

            return new TOTPChallengeInputModel { ReturnUrl = TOTPChallengeReturnUrl };
        }

        /// <summary>
        ///     public async Task<string> ManageTOTPChallengeVerification(TOTPChallengeInputModel inputModel)
        /// 
        ///     Manages the TOTP Challenge POST action.
        ///     
        ///     1. Checks if the current user is signed in. If not signed in, the method returns the RootRoute.
        ///     
        ///     2. If signed in, the ModelState is checked for validity and returns null as the redirect route if
        ///         the validation fails.
        ///         
        ///     3. If the validation succeeds, the user data is fetched using the UserManager.GetUserAsync()
        ///         method. If the user was not found, the RootRoute is returned as the redirect route.
        ///         
        ///     4. If the user was found, the TOTP code they submitted is verified using the
        ///         IdentityService.VerifyTOTPCode() method. If verification fails, an error message is added
        ///             to the model state and the method returns null.
        ///         
        ///     5. If verification succeeds, the return url to be added to the redirect route as a query string is
        ///         determined. If the returnUrl property of the inputModel is a valid return url, then that returnUrl is
        ///             selected. If it is not valid, then the RootRoute is used as the return url.
        ///             
        ///     6. Finally, the redirect route is determined calling the IdentityService.ManageTOTPChallengeSuccess()
        ///         method and this redirect route is returned from this method.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model containing the return url and user submitted TOTP code.
        /// </param>
        /// <returns>
        ///     The route to redirect to after the TOTP challenge verification process.
        /// </returns>
        public async Task<string> ManageTOTPChallengeVerification(TOTPChallengeInputModel inputModel)
        {
            bool currentUserSignedIn = IdentityService.CheckActiveSession();

            if (!currentUserSignedIn)
            {
                return RootRoute;
            }

            if (!ActionContext.ModelState.IsValid)
            {
                return null;
            }

            ApplicationUser user = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            if (user == null)
            {
                // user doesn't exist, redirect to Root route
                return RootRoute;
            }
            else
            {
                bool totpCodeVerified = await IdentityService.VerifyTOTPCode(
                    user,
                    TokenOptions.DefaultAuthenticatorProvider,
                    inputModel.VerificationCode
                );

                if (!totpCodeVerified)
                {
                    ActionContext.ModelState.AddModelError(string.Empty, "Invalid verification code");

                    return null;
                }
                else
                {
                    string returnUrl = RootRoute;

                    if (IsValidReturnUrl(inputModel.ReturnUrl, InteractionService, RouteEndpointService.EndpointRoutes))
                    {
                        returnUrl = $"~{inputModel.ReturnUrl}";
                    }

                    string redirectRoute = await IdentityService.ManageTOTPChallengeSuccess(
                        user,
                        null,
                        UserActionContexts.TOTPChallenge,
                        returnUrl
                    );

                    return redirectRoute;
                }
            }
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
