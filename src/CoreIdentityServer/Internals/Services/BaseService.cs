using System;
using System.Collections.Generic;
using CoreIdentityServer.Internals.Constants.Account;
using CoreIdentityServer.Internals.Constants.Storage;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Internals.Services
{
    public abstract class BaseService
    {
        private protected ActionContext ActionContext;
        private protected IConfiguration Configuration;
        private protected ITempDataDictionary TempData;

        public BaseService()
        {}

        public BaseService(
            IActionContextAccessor actionContextAccessor,
            IConfiguration configuration,
            ITempDataDictionaryFactory tempDataDictionaryFactory
        ) {
            ActionContext = actionContextAccessor.ActionContext;
            Configuration = configuration;
            TempData = tempDataDictionaryFactory.GetTempData(ActionContext.HttpContext);
        }

        /// <summary>
        ///     private protected void AddCaptchaSiteKeyToTempData()
        ///     
        ///     Calls the base method AddOrRetainTempData() to add the captcha site key
        ///         to the TempData so it can be accessed in the view.
        /// </summary>
        public void AddCaptchaSiteKeyToTempData()
        {
            AddOrRetainTempData(
                TempDataKeys.CloudflareCaptchaSiteKey,
                Configuration["cloudflare_captcha:site_key"]
            );
        }

        /// <summary>
        ///     private protected AddOrRetainTempData()
        ///     
        ///     Adds a TempData so it can be accessed in the view.
        ///     
        ///     If the TempData already exists, then this method retains it.
        /// </summary>
        private protected void AddOrRetainTempData(string key, object value)
        {
            if (TempData.ContainsKey(key))
            {
                TempData.Keep(key);
            }
            else
            {
                TempData[key] = value;
            }
        }

        /// <summary>
        ///     Sets a DateTime object in the TempData that acts as the expiry
        ///         date/time for the TempData.
        /// </summary>
        /// <param name="TempData">The ITempDataDictionary TempData</param>
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


        /// <summary>
        ///     private protected object[] GenerateArray(params object[] items)
        ///     
        ///     Generates an array of objects from the provided parameters.
        /// </summary>
        /// <param name="items">Parameters/objects</param>
        /// <returns>An array of the parameters</returns>
        private protected object[] GenerateArray(params object[] items)
        {
            return items;
        }


        /// <summary>
        ///     private protected string GenerateAbsoluteLocalUrl(
        ///         string action,
        ///         string controller,
        ///         string area
        ///     )
        ///     
        ///     Generates an absolute url with the provided information.
        /// </summary>
        /// <param name="action">Action name</param>
        /// <param name="controller">Controller name</param>
        /// <param name="area">Area name</param>
        /// <returns>A string representing an absolute url</returns>
        private protected string GenerateAbsoluteLocalUrl(
            string action,
            string controller,
            string area
        ) {
            string rootUrl = Config.GetApplicationUrl();

            return $"{rootUrl}/{area}/{controller}/{action}";
        }


        /// <summary>
        ///     private protected string GenerateRouteUrl(
        ///         string action, string controller, string area, string queryString = null
        ///     )
        ///     
        ///     Generates a url with the provided parameters.
        /// </summary>
        /// <param name="action">Action name</param>
        /// <param name="controller">Controller name</param>
        /// <param name="area">Area name</param>
        /// <param name="queryString">Query string to be added to the url</param>
        /// <returns>A string representing a url</returns>
        private protected string GenerateRouteUrl(string action, string controller, string area, string queryString = null)
        {
            string routeUrl = $"~/{area}/{controller}/{action}";

            return AddQueryString(routeUrl, queryString);
        }


        /// <summary>
        ///     private protected string GenerateRouteUrl(string route, string queryString)
        ///     
        ///     Generates a url with the provided parameters. This is an overload method.
        /// </summary>
        /// <param name="route">Route for the url</param>
        /// <param name="queryString">Query string to be added to the url</param>
        /// <returns>A string representing a url</returns>
        private protected string GenerateRouteUrl(string route, string queryString)
        {
            return AddQueryString(route, queryString);
        }


        // Adds a query string to the url.
        private string AddQueryString(string routeUrl, string queryString)
        {
            if (!string.IsNullOrWhiteSpace(queryString))
                return $"{routeUrl}?{queryString}";

            return routeUrl;
        }
    }
}
