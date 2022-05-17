using System;
using System.Collections.Generic;
using System.Linq;
using CoreIdentityServer.Internals.Constants.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Internals.Services
{
    public class RouteEndpointService : IDisposable
    {
        private IConfiguration Config;
        private EndpointDataSource EndpointDataSource;
        private ActionContext ActionContext;
        private IUrlHelper UrlHelper;
        public List<string> EndpointRoutes { get; private set; }
        public List<string> EndpointRoutesRequiringTOTPChallenge { get; private set; }

        public RouteEndpointService(
            IConfiguration config,
            EndpointDataSource endpointDataSource,
            IActionContextAccessor actionContextAccessor,
            IUrlHelperFactory urlHelperFactory
        ) {
            Config = config;
            EndpointDataSource = endpointDataSource;
            ActionContext = actionContextAccessor.ActionContext;
            UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);

            EndpointRoutes = new List<string>();
            EndpointRoutesRequiringTOTPChallenge = new List<string>();

            PopulateEndpointRoutes(endpointDataSource);
        }

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

            EndpointDataSource = null;
            ActionContext = null;
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
