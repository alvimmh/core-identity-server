using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Internals.Services
{
    public class RouteEndpointService : IDisposable
    {
        private IConfiguration Config;
        private EndpointDataSource EndpointDataSource;
        public List<string> EndpointRoutes { get; private set; }

        public RouteEndpointService(IConfiguration config, EndpointDataSource endpointDataSource) {
            Config = config;
            EndpointDataSource = endpointDataSource;
            EndpointRoutes = new List<string>();

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
                    string controllerName = (string)routeValues?["controller"];
                    string actionName = (string)routeValues?["action"];

                    string routeUrl = string.Join('/', $"/{areaName}", controllerName, actionName).ToLower();
                    EndpointRoutes.Add(routeUrl);
                }
            };
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
