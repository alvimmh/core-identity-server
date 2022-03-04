using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Authorization.Requirements;
using CoreIdentityServer.Internals.Constants.Account;
using CoreIdentityServer.Internals.Constants.Authorization;
using IdentityModel;
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
                Claim TOTPAuthorizationExpiryClaim = authorizationHandlerContext.User.FindFirst(ProjectClaimTypes.TOTPAuthorizationExpiry);
                Claim authenticationTimeClaim = authorizationHandlerContext.User.FindFirst(JwtClaimTypes.AuthenticationTime);

                bool userAuthorized = false;

                // check if the current time is within the duration of the TOTPAuthorizationExpiry claim's value
                if (TOTPAuthorizationExpiryClaim != null)
                {
                    DateTime authorizationExpiryDateTime = DateTime.Parse(TOTPAuthorizationExpiryClaim.Value);
                    userAuthorized = DateTime.UtcNow < authorizationExpiryDateTime;
                }

                // check if authentication was performed within the duration of the TOTPAuthorizationExpiry claim's value
                if (!userAuthorized && authenticationTimeClaim != null)
                {
                    long unixAuthenticationTime = long.Parse(authenticationTimeClaim.Value);
                    DateTime authenticationTime = DateTimeOffset.FromUnixTimeSeconds(unixAuthenticationTime).UtcDateTime;
                    userAuthorized = DateTime.UtcNow < authenticationTime.AddSeconds(AccountOptions.TOTPAuthorizationDurationInSeconds);
                }

                if (userAuthorized)
                {
                    authorizationHandlerContext.Succeed(requirement);
                }
            }

            return Task.FromResult(false);
        }
    }
}
