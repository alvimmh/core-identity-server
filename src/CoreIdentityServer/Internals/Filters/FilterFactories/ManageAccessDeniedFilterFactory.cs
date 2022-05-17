using System;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.FilterFactories
{
    public class ManageAccessDeniedFilterFactory : Attribute, IFilterFactory
    {
        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            ManageAccessDenied filter = new ManageAccessDenied();

            object routeEndpointService = serviceProvider.GetService(typeof(RouteEndpointService));
            
            filter.RouteEndpointService = routeEndpointService as RouteEndpointService;

            return filter;
        }
    }
}
