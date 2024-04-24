// Copyright (c) Duende Software. All rights reserved.
//
// By accessing the Duende IdentityServer code here, you are agreeing to the following licensing terms:
// https://duendesoftware.com/license/identityserver.pdf
//
// or, alternatively you can view the license in the project's ~/Licenses/DuendeSoftwareLicense.pdf file
//
// If you do not agree to these terms, do not access the Duende IdentityServer code.

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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Areas.Access.Services
{
    public class GrantsService : BaseService, IDisposable
    {
        private IdentityService IdentityService;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly IIdentityServerInteractionService InteractionService;
        private readonly IEventService EventService;
        private readonly IClientStore ClientStore;
        private readonly IResourceStore ResourceStore;
        private ActionContext ActionContext;
        private bool ResourcesDisposed;

        public GrantsService(
            IdentityService identityService,
            UserManager<ApplicationUser> userManager,
            IIdentityServerInteractionService interactionService,
            IEventService eventService,
            IClientStore clientStore,
            IResourceStore resourceStore,
            IActionContextAccessor actionContextAccessor
        ) {
            IdentityService = identityService;
            UserManager = userManager;
            InteractionService = interactionService;
            EventService = eventService;
            ClientStore = clientStore;
            ResourceStore = resourceStore;
            ActionContext = actionContextAccessor.ActionContext;
        }


        /// <summary>
        ///     public async Task<GrantsViewModel> ManageGrants()
        ///     
        ///     Manages the Index GET action. This utilizes the BuildViewModelAsync() method.
        /// </summary>
        /// <returns>A view model containing all of the user's grants</returns>
        public async Task<GrantsViewModel> ManageGrants()
        {
            GrantsViewModel viewModel = await BuildViewModelAsync();

            return viewModel;
        }


        /// <summary>
        ///     public async Task RevokeGrant(RevokeGrantInputModel inputModel)
        ///     
        ///     Manages the Index POST action. This method revokes a specific grant by the user.
        /// </summary>
        /// <param name="inputModel">
        ///     The object containing the client id for which the grant is being revoked
        /// </param>
        /// <returns>void</returns>
        public async Task RevokeGrant(RevokeGrantInputModel inputModel)
        {
            if (ActionContext.ModelState.IsValid)
            {
                IEnumerable<Grant> grants = await InteractionService.GetAllUserGrantsAsync();

                bool validGrantExists = grants.Any(grant => grant.ClientId == inputModel.ClientId);

                if (validGrantExists)
                {
                    ClaimsPrincipal userSession = ActionContext.HttpContext.User;

                    ApplicationUser user = await UserManager.GetUserAsync(userSession);

                    await InteractionService.RevokeUserConsentAsync(inputModel.ClientId);
                    await EventService.RaiseAsync(new GrantsRevokedEvent(userSession.GetSubjectId(), inputModel.ClientId));

                    // initiate signout so the client with revoked grant signs out the user
                    await IdentityService.SignOut();

                    if (user != null)
                    {
                        // since all sessions are signed out,
                        // user is automatically signed in again to prevent user go through sign in flow again
                        // this ensures that all revoked sessions are signed out while the current session is still signed in
                        await IdentityService.RefreshUserSignIn(user);
                    }
                }
            }

        }


        /// <summary>
        ///     private async Task<GrantsViewModel> BuildViewModelAsync()
        ///     
        ///     Creates the view model for the ManageGrants() method.
        /// </summary>
        /// <returns>A view model object containing all the grants of the user</returns>
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
