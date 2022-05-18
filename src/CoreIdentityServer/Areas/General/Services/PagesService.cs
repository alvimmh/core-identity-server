using System;
using CoreIdentityServer.Internals.Services;
using Duende.IdentityServer.Services;

namespace CoreIdentityServer.Areas.General.Services
{
    public class PagesService : BaseService, IDisposable
    {
        private readonly IIdentityServerInteractionService InteractionService;
        private readonly RouteEndpointService RouteEndpointService;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public PagesService(
            IIdentityServerInteractionService interactionService,
            RouteEndpointService routeEndpointService
        ) {
            InteractionService = interactionService;
            RouteEndpointService = routeEndpointService;
            RootRoute = GenerateRouteUrl("Dashboard", "Pages", "General");
        }

        public string ManageDashboard(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
                return null;

            bool returnUrlValid = IsValidReturnUrl(
                returnUrl, InteractionService, RouteEndpointService.EndpointRoutes
            );

            if (!returnUrlValid)
                return null;

            return $"~{returnUrl}";
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
