using System;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Duende.IdentityServer.Services;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models.Consent;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using Duende.IdentityServer.Validation;
using CoreIdentityServer.Internals.Constants.Consent;
using IdentityModel;
using Duende.IdentityServer.Events;
using CoreIdentityServer.Internals.Extensions;
using System.Security.Claims;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CoreIdentityServer.Internals.Constants.Storage;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class ConsentService : BaseService, IDisposable
    {
        private ActionContext ActionContext;
        private readonly IIdentityServerInteractionService InteractionService;
        private readonly IEventService EventService;
        private readonly ITempDataDictionary TempData;
        private readonly ILogger<ConsentService> Logger;
        private bool ResourcesDisposed;

        public ConsentService(
            IActionContextAccessor actionContextAccessor,
            IIdentityServerInteractionService interactionService,
            IEventService eventService,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            ILogger<ConsentService> logger
        ) {
            ActionContext = actionContextAccessor.ActionContext;
            InteractionService = interactionService;
            EventService = eventService;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
            Logger = logger;
        }


        /// <summary>
        ///     public async Task<ConsentViewModel> ManageConsent(string returnUrl)
        /// 
        ///     Manages the Consent Controller's Index GET action.
        ///     
        ///     1. Builds a view model using the BuildViewModelAsync() method by passing
        ///         the returnUrl param to it.
        ///     
        ///     2. If the BuildViewModelAsync() method returns null, an attempt to retrieve
        ///         a consent return url from the TempData is made. If it was retrieved,
        ///             a view model is constructed with the BuildViewModelAsync() method,
        ///                 this time passing the retrieved consent return url to it.
        ///                 
        ///     3. Finally, the method returns a view model constructed using step 1 or 2, or,
        ///         null if it was not possible to constuct it.
        /// </summary>
        /// <param name="returnUrl">The url to return to</param>
        /// <returns>
        ///     A ConsentViewModel containing properties such as ClientName, ClientUrl,
        ///         ClientLogoUrl, AllowRememberConsent, IdentityScopes and ApiScopes
        ///             or,
        ///                 null.
        /// </returns>
        public async Task<ConsentViewModel> ManageConsent(string returnUrl)
        {
            ConsentViewModel viewModel = await BuildViewModelAsync(returnUrl);

            if (viewModel == null)
            {
                bool consentReturnUrlExists = TempData.TryGetValue(
                    TempDataKeys.ConsentReturnUrl,
                    out object consentReturnUrlTempData
                );

                string consentReturnUrl = consentReturnUrlExists ? consentReturnUrlTempData.ToString() : null;

                if (!string.IsNullOrWhiteSpace(consentReturnUrl))
                {
                    viewModel = await BuildViewModelAsync(consentReturnUrl);
                }
            }

            return viewModel;
        }


        /// <summary>
        ///     public async Task<object[]> ManageConsentResponse(ConsentInputModel inputModel)
        /// 
        ///     Manages the Consent Controller's Index POST action.
        ///     
        ///     1. Builds an object containing the consent result using the method ProcessConsent().
        ///     
        ///     2. If the result instructs to redirect, the method returns the consent result object
        ///         and a boolean indicating a native redirect, in an array of objects.
        ///                 
        ///     3. If the redirection is not necessary, the ModelState is checked for validation
        ///         errors and any found error is added to the ModelState for the user.
        ///         
        ///     4. Finally, the method returns an array of objects containing the consent result and
        ///         the native redirect indicating boolean.
        /// </summary>
        /// <param name="inputModel">The input model containing the consent details from the user</param>
        /// <returns>
        ///     An array of objects containing the consent result and the native redirect boolean indicator.
        /// </returns>
        public async Task<object[]> ManageConsentResponse(ConsentInputModel inputModel)
        {
            bool nativeRedirect = false;
            ProcessConsentResult consentResult = await ProcessConsent(inputModel);

            if (consentResult.IsRedirect)
            {
                AuthorizationRequest context = await InteractionService.GetAuthorizationContextAsync(inputModel.ReturnUrl);

                if (context?.IsNativeClient() == true)
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    nativeRedirect = true;
                }

                return GenerateArray(consentResult, nativeRedirect);
            }

            if (consentResult.HasValidationError)
                ActionContext.ModelState.AddModelError(string.Empty, consentResult.ValidationError);

            return GenerateArray(consentResult, nativeRedirect);
        }


        /// <summary>
        ///     private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel inputModel)
        ///     
        ///     Processes the user's consent.
        /// </summary>
        /// <param name="inputModel">The input model containing the user's consent</param>
        /// <returns>
        ///     The ProcessConsentResult object containing the created ConsentViewModel and other
        ///         necesary information.
        /// </returns>
        private async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel inputModel)
        {
            ProcessConsentResult result = new ProcessConsentResult();

            // validate return url is still valid
            AuthorizationRequest authorizationRequest = await InteractionService.GetAuthorizationContextAsync(inputModel.ReturnUrl);

            if (authorizationRequest == null)
                return result;

            ConsentResponse grantedConsent = null;
            ClaimsPrincipal user = ActionContext.HttpContext.User;

            // user clicked 'no' - send back the standard 'access_denied' response
            if (inputModel?.Button == "no")
            {
                grantedConsent = new ConsentResponse { Error = AuthorizationError.AccessDenied };

                // emit event
                await EventService.RaiseAsync(new ConsentDeniedEvent(
                    user.GetSubjectId(),
                    authorizationRequest.Client.ClientId,
                    authorizationRequest.ValidatedResources.RawScopeValues
                ));
            }
            // user clicked 'yes' - validate the data
            else if (inputModel?.Button == "yes")
            {
                // if the user consented to some scope, build the response model
                if (inputModel.ScopesConsented != null && inputModel.ScopesConsented.Any())
                {
                    IEnumerable<string> scopes = inputModel.ScopesConsented;

                    if (ConsentOptions.EnableOfflineAccess == false)
                        scopes = scopes.Where(x => x != Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess);

                    grantedConsent = new ConsentResponse
                    {
                        RememberConsent = inputModel.RememberConsent,
                        ScopesValuesConsented = scopes.ToArray(),
                        Description = inputModel.Description
                    };

                    // emit event
                    await EventService.RaiseAsync(
                        new ConsentGrantedEvent(
                            user.GetSubjectId(),
                            authorizationRequest.Client.ClientId,
                            authorizationRequest.ValidatedResources.RawScopeValues,
                            grantedConsent.ScopesValuesConsented,
                            grantedConsent.RememberConsent
                        )
                    );
                }
                else
                {
                    result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
                }
            }
            else
            {
                result.ValidationError = ConsentOptions.InvalidSelectionErrorMessage;
            }

            if (grantedConsent != null)
            {
                // communicate outcome of consent back to identityserver
                await InteractionService.GrantConsentAsync(authorizationRequest, grantedConsent);

                // remove any stored tempdata
                TempData.Clear();

                // indicate that's it ok to redirect back to authorization endpoint
                result.RedirectUri = inputModel.ReturnUrl;
                result.Client = authorizationRequest.Client;
            }
            else
            {
                // we need to redisplay the consent UI
                result.ViewModel = await BuildViewModelAsync(inputModel.ReturnUrl, inputModel);
            }

            return result;
        }


        /// <summary>
        ///     private async Task<ConsentViewModel> BuildViewModelAsync(
        ///         string returnUrl, ConsentInputModel inputModel = null
        ///     )
        ///     
        ///     Builds the ConsentViewModel object. It utilizes the CreateConsentViewModel() method.
        /// </summary>
        /// <param name="returnUrl">The url to return to</param>
        /// <param name="inputModel">The input model object containing the user's consent</param>
        /// <returns>The created ConsentViewModel object</returns>
        private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel inputModel = null)
        {
            AuthorizationRequest authorizationRequest = await InteractionService.GetAuthorizationContextAsync(returnUrl);

            if (authorizationRequest != null)
            {
                TempData[TempDataKeys.ConsentReturnUrl] = returnUrl;

                return CreateConsentViewModel(inputModel, returnUrl, authorizationRequest);
            }
            else
            {
                Logger.LogError("No consent request matching request: {0}", returnUrl);
            }

            return null;
        }


        /// <summary>
        ///     private ConsentViewModel CreateConsentViewModel(
        ///         ConsentInputModel inputModel,
        ///         string returnUrl,
        ///         AuthorizationRequest authorizationRequest
        ///     )
        ///     
        ///     Creates the ConsentViewModel object.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model containing information from the customer's consent
        /// </param>
        /// <param name="returnUrl">
        ///     The url to return to
        /// </param>
        /// <param name="authorizationRequest">
        ///     The authorization for which the user's consent was required
        /// </param>
        /// <returns>The ConsentViewModel object</returns>
        private ConsentViewModel CreateConsentViewModel(
            ConsentInputModel inputModel,
            string returnUrl,
            AuthorizationRequest authorizationRequest
        ) {
            ConsentViewModel viewModel = new ConsentViewModel
            {
                RememberConsent = inputModel?.RememberConsent ?? true,
                ScopesConsented = inputModel?.ScopesConsented ?? Enumerable.Empty<string>(),
                Description = inputModel?.Description,

                ReturnUrl = returnUrl,

                ClientName = authorizationRequest.Client.ClientName ?? authorizationRequest.Client.ClientId,
                ClientUrl = authorizationRequest.Client.ClientUri,
                ClientLogoUrl = authorizationRequest.Client.LogoUri,
                AllowRememberConsent = authorizationRequest.Client.AllowRememberConsent
            };

            viewModel.IdentityScopes = authorizationRequest.ValidatedResources.Resources.IdentityResources
                .Select(x => CreateScopeViewModel(x, viewModel.ScopesConsented.Contains(x.Name) || inputModel == null))
                .ToArray();

            IEnumerable<string> resourceIndicators = authorizationRequest.Parameters.GetValues(OidcConstants.AuthorizeRequest.Resource) ?? Enumerable.Empty<string>();
            IEnumerable<ApiResource> apiResources = authorizationRequest.ValidatedResources.Resources.ApiResources.Where(x => resourceIndicators.Contains(x.Name));

            List<ScopeViewModel> apiScopes = new List<ScopeViewModel>();

            foreach (ParsedScopeValue parsedScope in authorizationRequest.ValidatedResources.ParsedScopes)
            {
                ApiScope apiScope = authorizationRequest.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);

                if (apiScope != null)
                {
                    ScopeViewModel scopeViewModel = CreateScopeViewModel(parsedScope, apiScope, viewModel.ScopesConsented.Contains(parsedScope.RawValue) || inputModel == null);

                    scopeViewModel.Resources = apiResources.Where(x => x.Scopes.Contains(parsedScope.ParsedName))
                        .Select(x => new ResourceViewModel
                        {
                            Name = x.Name,
                            DisplayName = x.DisplayName ?? x.Name,
                        }).ToArray();

                    apiScopes.Add(scopeViewModel);
                }
            }

            if (ConsentOptions.EnableOfflineAccess && authorizationRequest.ValidatedResources.Resources.OfflineAccess)
                apiScopes.Add(GetOfflineAccessScope(viewModel.ScopesConsented.Contains(Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess) || inputModel == null));

            viewModel.ApiScopes = apiScopes;

            return viewModel;
        }


        /// <summary>
        ///     private ScopeViewModel CreateScopeViewModel(IdentityResource identityResource, bool check)
        ///     
        ///     Creates a ScopeViewModel object for user identity resource.
        /// </summary>
        /// <param name="identityResource">The identity resource to create the view model for</param>
        /// <param name="check">Boolean indicating whether it is a required scope</param>
        /// <returns>The created object</returns>
        private ScopeViewModel CreateScopeViewModel(IdentityResource identityResource, bool check)
        {
            return new ScopeViewModel
            {
                Name = identityResource.Name,
                Value = identityResource.Name,
                DisplayName = identityResource.DisplayName ?? identityResource.Name,
                Description = identityResource.Description,
                Emphasize = identityResource.Emphasize,
                Required = identityResource.Required,
                Checked = check || identityResource.Required
            };
        }


        /// <summary>
        ///     public ScopeViewModel CreateScopeViewModel(
        ///         ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check
        ///     )
        ///     
        ///     Creates a ScopeViewModel for a parsed scope.
        /// </summary>
        /// <param name="parsedScopeValue">Value of the parsed scope</param>
        /// <param name="apiScope">The API scope</param>
        /// <param name="check">Boolean indicating if the scope being created is a required one</param>
        /// <returns>The created view model object</returns>
        public ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check)
        {
            string displayName = apiScope.DisplayName ?? apiScope.Name;

            if (!String.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter))
            {
                displayName += ":" + parsedScopeValue.ParsedParameter;
            }

            return new ScopeViewModel
            {
                Name = parsedScopeValue.ParsedName,
                Value = parsedScopeValue.RawValue,
                DisplayName = displayName,
                Description = apiScope.Description,
                Emphasize = apiScope.Emphasize,
                Required = apiScope.Required,
                Checked = check || apiScope.Required
            };
        }


        /// <summary>
        ///     private ScopeViewModel GetOfflineAccessScope(bool check)
        ///     
        ///     Creates a ScopeViewModel containing offline access scope.
        /// </summary>
        /// <param name="check">Boolean indicating the status of the checkbox for offline access</param>
        /// <returns>The ScopeViewModel object</returns>
        private ScopeViewModel GetOfflineAccessScope(bool check)
        {
            return new ScopeViewModel
            {
                Value = Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = ConsentOptions.OfflineAccessDisplayName,
                Description = ConsentOptions.OfflineAccessDescription,
                Emphasize = true,
                Checked = check
            };
        }

        // clean up to be done by DI
        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            ResourcesDisposed = true;
        }
    }
}
