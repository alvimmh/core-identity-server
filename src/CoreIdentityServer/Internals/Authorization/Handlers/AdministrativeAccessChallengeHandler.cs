using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Authorization.Requirements;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace CoreIdentityServer.Internals.Authorization.Handlers
{
    public class AdministrativeAccessChallengeHandler : AuthorizationHandler<AdministrativeAccessChallengeRequirement>
    {
        private HttpContext HttpContext;
        private readonly ITokenValidator TokenValidator;

        public AdministrativeAccessChallengeHandler(
            IHttpContextAccessor httpContextAccessor,
            ITokenValidator tokenValidator
        ) {
            HttpContext = httpContextAccessor.HttpContext;
            TokenValidator = tokenValidator;
        }


        /// <summary>
        ///     HandleRequirementAsync(
        ///         AuthorizationHandlerContext authorizationHandlerContext,
        ///         AdministrativeAccessChallengeRequirement requirement
        ///     )
        ///     
        ///     This authorization handler is used to authorize users of the clients
        ///     of the identity server by checking if their access token has the
        ///     administrative_access scope.
        /// </summary>
        /// <param name="authorizationHandlerContext"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        /// 
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext authorizationHandlerContext,
            AdministrativeAccessChallengeRequirement requirement
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
