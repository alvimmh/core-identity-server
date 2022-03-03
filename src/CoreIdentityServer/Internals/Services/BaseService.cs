using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing;

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

        private protected bool ValidateModel(object model)
        {
            ValidationContext validationContext = new ValidationContext(model);
            List<ValidationResult> validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults);

            return isValid;
        }

        private string AddQueryString(string routeUrl, string queryString)
        {
            if (!string.IsNullOrWhiteSpace(queryString))
                return $"{routeUrl}?{queryString}";

            return routeUrl;
        }
    }
}
