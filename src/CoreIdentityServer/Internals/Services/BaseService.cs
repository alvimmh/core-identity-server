namespace CoreIdentityServer.Internals.Services
{
    public abstract class BaseService
    {
        private protected object[] GenerateArray(params object[] items)
        {
            return items;
        }

        private protected string GenerateRouteUrl(string action, string controller, string area, string queryString = null)
        {
            string routeUrl = $"~/{area}/{controller}/{action}";

            return AddQueryString(routeUrl, queryString);
        }

        private protected string GenerateRouteUrl(string route, string queryString)
        {
            return AddQueryString(route, queryString);
        }

        private string AddQueryString(string routeUrl, string queryString)
        {
            if (!string.IsNullOrWhiteSpace(queryString))
                return $"{routeUrl}?{queryString}";

            return routeUrl;
        }
    }
}
