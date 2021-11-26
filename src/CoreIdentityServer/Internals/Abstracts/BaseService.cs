using Microsoft.AspNetCore.Routing;

namespace CoreIdentityServer.Internals.Abstracts
{
    public abstract class BaseService
    {
        private protected RouteValueDictionary GenerateRedirectRouteValues(string action, string controller, string area)
        {
            return new RouteValueDictionary(
                new {
                    action,
                    controller,
                    area
                }
            );
        }
    }
}
