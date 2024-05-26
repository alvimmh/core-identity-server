using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.AuthorizationPolicies.Requirements;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CoreIdentityServer.Internals.AuthorizationPolicies.Handlers
{
    public class AdministrativeAccessHandler : AuthorizationHandler<AdministrativeAccessRequirement>
    {
        private HttpContext HttpContext;
        private readonly ITokenValidator TokenValidator;

        public AdministrativeAccessHandler(
            IHttpContextAccessor httpContextAccessor,
            ITokenValidator tokenValidator
        ) {
            HttpContext = httpContextAccessor.HttpContext;
            TokenValidator = tokenValidator;
        }


        /// <summary>
        ///     protected override async Task HandleRequirementAsync(
        ///         AuthorizationHandlerContext authorizationHandlerContext,
        ///         AdministrativeAccessRequirement requirement
        ///     )
        ///     
        ///     This authorization handler is used to authorize users of the clients
        ///     of this identity server by checking if their access token has the
        ///     administrative_access scope.
        /// </summary>
        /// <param name="authorizationHandlerContext">Context for the handler</param>
        /// <param name="requirement">The requirement for the authorization handler</param>
        /// <returns>void</returns>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext authorizationHandlerContext,
            AdministrativeAccessRequirement requirement
        ) {
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    string accessToken = authorizationHeader.Substring("Bearer ".Length).Trim();

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        string expectedScope = "administrative_access";

                        TokenValidationResult accessTokenValidationResult = await TokenValidator.ValidateAccessTokenAsync(accessToken, expectedScope);

                        if (!accessTokenValidationResult.IsError)
                            authorizationHandlerContext.Succeed(requirement);
                    }
                }
            }
        }
    }
}
