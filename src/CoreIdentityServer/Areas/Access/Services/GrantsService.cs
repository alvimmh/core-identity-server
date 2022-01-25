using System;
using CoreIdentityServer.Internals.Services;
using Duende.IdentityServer.Services;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Access.Models.Grants;
using System.Collections.Generic;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using System.Linq;
using Duende.IdentityServer.Events;
using System.Security.Claims;
using Duende.IdentityServer.Extensions;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class GrantsService : BaseService, IDisposable
    {
        private readonly IIdentityServerInteractionService InteractionService;
        private readonly IEventService EventService;
        private readonly IClientStore ClientStore;
        private readonly IResourceStore ResourceStore;
        private bool ResourcesDisposed;

        public GrantsService(
            IIdentityServerInteractionService interactionService,
            IEventService eventService,
            IClientStore clientStore,
            IResourceStore resourceStore
        ) {
            InteractionService = interactionService;
            EventService = eventService;
            ClientStore = clientStore;
            ResourceStore = resourceStore;
        }

        public async Task<GrantsViewModel> ManageGrants()
        {
            GrantsViewModel viewModel = await BuildViewModelAsync();

            return viewModel;
        }

        public async Task RevokeGrant(string clientId, ClaimsPrincipal user)
        {
            await InteractionService.RevokeUserConsentAsync(clientId);
            await EventService.RaiseAsync(new GrantsRevokedEvent(user.GetSubjectId(), clientId));
        }

        private async Task<GrantsViewModel> BuildViewModelAsync()
        {
            IEnumerable<Grant> grants = await InteractionService.GetAllUserGrantsAsync();

            List<GrantViewModel> grantsList = new List<GrantViewModel>();

            foreach(Grant grant in grants)
            {
                Client client = await ClientStore.FindClientByIdAsync(grant.ClientId);

                if (client != null)
                {
                    Resources resources = await ResourceStore.FindResourcesByScopeAsync(grant.Scopes);

                    var item = new GrantViewModel()
                    {
                        ClientId = client.ClientId,
                        ClientName = client.ClientName ?? client.ClientId,
                        ClientLogoUrl = client.LogoUri,
                        ClientUrl = client.ClientUri,
                        Description = grant.Description,
                        Created = grant.CreationTime,
                        Expires = grant.Expiration,
                        IdentityGrantNames = resources.IdentityResources.Select(x => x.DisplayName ?? x.Name).ToArray(),
                        ApiGrantNames = resources.ApiScopes.Select(x => x.DisplayName ?? x.Name).ToArray()
                    };

                    grantsList.Add(item);
                }
            }

            return new GrantsViewModel
            {
                Grants = grantsList
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
