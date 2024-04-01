using System;
using System.Collections.Generic;
using CoreIdentityServer.Internals.Constants.Account;
using CoreIdentityServer.Internals.Constants.Storage;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CoreIdentityServer.Internals.Services
{
    public abstract class BaseService
    {
        private protected void SetTempDataExpiryDateTime(ITempDataDictionary TempData)
        {
            DateTime expiryDateTime = DateTime.UtcNow.AddSeconds(AccountOptions.TempDataLifetimeInSeconds);

            TempData[TempDataKeys.TempDataExpiryDateTime] = expiryDateTime;
        }


        /// <summary>
        ///     bool IsValidReturnUrl(
        ///         string returnUrl,
        ///         IIdentityServerInteractionService duendeServerInteractionService,
        ///         List<string> routeEndpoints
        ///     )
        ///     
        ///     Checks whether a return url is valid for redirect.
        ///     
        ///     1. Initially it checks if the return url is empty and if Duende Server Interaction
        ///         service validates the return url.
        ///     
        ///     2. If this check fails, it checks against the list of route endpoints of this application
        ///     
        ///     3. If this check fails, then the return url is stripped off of its query string and any route
        ///         attributes and checked again.
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="duendeServerInteractionService"></param>
        /// <param name="routeEndpoints"></param>
        /// <returns>boolean for the validity of the return url</returns>
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
                int returnUrlSubstringEndIndex = returnUrlQueryStringStartIndex;
                
                if (returnUrlQueryStringStartIndex == -1)
                {
                    // check for route attributes
                    int returnUrlRouteAttributeStartIndex = returnUrl.LastIndexOf('/');

                    returnUrlSubstringEndIndex = returnUrlRouteAttributeStartIndex != -1 ? returnUrlRouteAttributeStartIndex : returnUrlSubstringEndIndex;
                }

                if (returnUrlSubstringEndIndex == -1)
                {
                    return false;
                }
                else
                {
                    string returnUrlPath = returnUrl.Substring(0, returnUrlSubstringEndIndex);

                    return routeEndpoints.Contains(returnUrlPath.ToLower());
                }
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

        private protected string GenerateAbsoluteLocalUrl(
            string action,
            string controller,
            string area
        ) {
            string rootUrl = Config.GetApplicationUrl();

            return $"{rootUrl}/{area}/{controller}/{action}";
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
