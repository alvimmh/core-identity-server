using System;
using System.Collections.Generic;
using CoreIdentityServer.Internals.Constants.Account;
using CoreIdentityServer.Internals.Constants.Storage;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Internals.Services
{
    public abstract class BaseService
    {
        private protected void SetTempDataExpiryDateTime(ITempDataDictionary TempData)
        {
            DateTime expiryDateTime = DateTime.UtcNow.AddSeconds(AccountOptions.TempDataLifetimeInSeconds);

            TempData[TempDataKeys.TempDataExpiryDateTime] = expiryDateTime;
        }

        private protected bool IsValidReturnUrl(
            string returnUrl,
            IIdentityServerInteractionService duendeServerInteractionService,
            List<string> routeEndpoints
        ) {
            bool isReturnUrlNotEmpty = !string.IsNullOrWhiteSpace(returnUrl);

            bool isValidReturnUrl = isReturnUrlNotEmpty && duendeServerInteractionService.IsValidReturnUrl(returnUrl);

            if (isValidReturnUrl)
            {
                return true;
            }
            else if (isReturnUrlNotEmpty && !isValidReturnUrl && routeEndpoints.Contains(returnUrl.ToLower()))
            {
                return true;
            }
            else if (isReturnUrlNotEmpty && !isValidReturnUrl)
            {
                int returnUrlQueryStringStartIndex = returnUrl.IndexOf('?');

                string returnUrlPath = returnUrl.Substring(0, returnUrlQueryStringStartIndex);

                return routeEndpoints.Contains(returnUrlPath.ToLower());
            }
            else
            {
                return false;
            }
        }

        private protected object[] GenerateArray(params object[] items)
        {
            return items;
        }

        private protected string GenerateAbsoluteLocalUrl(string action, string controller, string area, IConfiguration config)
        {
            return $"{config["ApplicationUrl"]}/{area}/{controller}/{action}";
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
