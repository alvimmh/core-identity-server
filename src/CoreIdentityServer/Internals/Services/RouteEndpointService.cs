using System;
using System.Collections.Generic;
using System.Linq;
using CoreIdentityServer.Internals.Constants.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace CoreIdentityServer.Internals.Services
{
    // Class facilitating route enumeration
    public class RouteEndpointService : IDisposable
    {
        private IUrlHelper UrlHelper;
        public List<string> EndpointRoutes { get; private set; }
        public List<string> EndpointRoutesRequiringTOTPChallenge { get; private set; }

        public RouteEndpointService(
            EndpointDataSource endpointDataSource,
            IActionContextAccessor actionContextAccessor,
            IUrlHelperFactory urlHelperFactory
        ) {
            UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);

            EndpointRoutes = new List<string>();
            EndpointRoutesRequiringTOTPChallenge = new List<string>();

            PopulateEndpointRoutes(endpointDataSource);
        }


        /// <summary>
        ///     private void PopulateEndpointRoutes(EndpointDataSource endpointDataSource)
        ///     
        ///     Enumerates all routes and saves them in the class for usage.
        ///         The class is registered as a singleton service for the application.
        ///     
        ///     1. Loops over all endpoints and adds them to the EndpointRoutes property of
        ///         this class. Routes are lower-cased before adding.
        ///         
        ///     2. Endpoints that require a TOTP challenge are stored in a separate property.
        /// </summary>
        /// <param name="endpointDataSource">Source for endpoint instances</param>
        private void PopulateEndpointRoutes(EndpointDataSource endpointDataSource)
        {
            IEnumerable<RouteEndpoint> dataSourceRouteEndpoints = endpointDataSource.Endpoints.Cast<RouteEndpoint>();

            foreach (RouteEndpoint routeEndpoint in dataSourceRouteEndpoints)
            {
                #nullable enable
                IReadOnlyDictionary<string, object?> routeValues = routeEndpoint.RoutePattern.RequiredValues;
                #nullable disable

                string areaName = (string)(((routeValues["area"] is string) && routeValues["area"] != null) ? routeValues["area"] : null);

                // all CIS routes follow the area/controller/action pattern
                if (!string.IsNullOrWhiteSpace(areaName))
                {
                    string routePath = UrlHelper.RouteUrl(routeEndpoint.RoutePattern.RequiredValues).ToLower();

                    EndpointRoutes.Add(routePath);

                    bool routeEndpointRequiresTOTPChallenge = routeEndpoint
                                                                .Metadata
                                                                .OfType<AuthorizeAttribute>()
                                                                .Any(metadata => metadata.Policy == Policies.TOTPChallenge);

                    if (routeEndpointRequiresTOTPChallenge)
                        EndpointRoutesRequiringTOTPChallenge.Add(routePath);
                }
            };

            UrlHelper = null;
        }

        public void Dispose()
        {
            // clean up
            if (EndpointRoutes != null)
            {
                EndpointRoutes = null;
            }
        }
    }
}
