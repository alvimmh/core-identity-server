using System.Web;
using CoreIdentityServer.Internals.Authorization.Handlers;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.ActionFilters
{
    public class ManageAccessDenied : ActionFilterAttribute
    {
        public RouteEndpointService RouteEndpointService { get; set; }

        public ManageAccessDenied()
        {}

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            bool returnUrlAvailable = context.ActionArguments.TryGetValue("returnUrl", out object returnUrl);

            if (returnUrlAvailable)
            {
                bool returnUrlRequiresTOTPChallenge = RouteEndpointService.EndpointRoutesRequiringTOTPChallenge.Contains((string)returnUrl);
                bool userHasTOTPAuthorization = returnUrlRequiresTOTPChallenge ? TOTPChallengeHandler.IsUserAuthorized(context.HttpContext.User) : false;

                if (returnUrlRequiresTOTPChallenge && !userHasTOTPAuthorization)
                {
                    string encodedReturnUrl = HttpUtility.UrlEncode((string)returnUrl);

                    context.Result = new RedirectResult($"~/access/authentication/totpchallenge?returnUrl={encodedReturnUrl}");
                }
            }
        }
    }
}
