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


        /// <summary>
        ///     public string ManageDashboard(string returnUrl)
        ///     
        ///     Manages the Dashboard GET action.
        ///     
        ///     1. Checks if the return url is valid, if not, the method returns null.
        ///     
        ///     2. If valid, the method returns the return url, to which the user will
        ///         be redirected to instead of the Dashboard page.
        /// </summary>
        /// <param name="returnUrl">The url to return to</param>
        /// <returns>A return url or null</returns>
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
