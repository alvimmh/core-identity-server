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

namespace CoreIdentityServer.Areas.Access.Services
{
    public class ConsentService : BaseService, IDisposable
    {
        private ActionContext ActionContext;
        private readonly IIdentityServerInteractionService InteractionService;
        private readonly IEventService EventService;
        private readonly ILogger<ConsentService> Logger;
        private bool ResourcesDisposed;

        public ConsentService(
            IActionContextAccessor actionContextAccessor,
            IIdentityServerInteractionService interactionService,
            IEventService eventService,
            ILogger<ConsentService> logger
        ) {
            ActionContext = actionContextAccessor.ActionContext;
            InteractionService = interactionService;
            EventService = eventService;
            Logger = logger;
        }

        public async Task<ConsentViewModel> ManageConsent(string returnUrl)
        {
            ConsentViewModel viewModel = await BuildViewModelAsync(returnUrl);

            return viewModel;
        }

        public async Task<object[]> UpdateConsent(ConsentInputModel inputModel)
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

        private async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel inputModel = null)
        {
            AuthorizationRequest authorizationRequest = await InteractionService.GetAuthorizationContextAsync(returnUrl);

            if (authorizationRequest != null)
            {
                return CreateConsentViewModel(inputModel, returnUrl, authorizationRequest);
            }
            else
            {
                Logger.LogError("No consent request matching request: {0}", returnUrl);
            }

            return null;
        }

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
