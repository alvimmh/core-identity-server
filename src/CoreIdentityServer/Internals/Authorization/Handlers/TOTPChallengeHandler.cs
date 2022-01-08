using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Authorization.Requirements;
using CoreIdentityServer.Internals.Constants.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.Authorization.Handlers
{
    public class TOTPChallengeHandler : AuthorizationHandler<TOTPChallengeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext authorizationHandlerContext, TOTPChallengeRequirement requirement)
        {
            if (authorizationHandlerContext.User.Identity.IsAuthenticated)
            {
                // claim used to store expiry DateTime of TOTP authorization period
                Claim TOTPAuthorizationClaim = authorizationHandlerContext.User.FindFirst(Claims.TOTPAuthorizationExpiry);
                bool userAuthorized = false;

                if (TOTPAuthorizationClaim != null)
                {
                    DateTime authorizationExpiryDateTime = DateTime.Parse(TOTPAuthorizationClaim.Value);
                    userAuthorized = DateTime.UtcNow < authorizationExpiryDateTime;

                    if (userAuthorized)
                    {
                        authorizationHandlerContext.Succeed(requirement);
                    }
                }
            }

            return Task.FromResult(false);
        }
    }
}
