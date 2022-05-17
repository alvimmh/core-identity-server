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
                bool userAuthorized = IsUserAuthorized(authorizationHandlerContext.User);

                if (userAuthorized)
                {
                    authorizationHandlerContext.Succeed(requirement);
                }
            }

            return Task.FromResult(false);
        }

        public static Claim GetTOTPAuthorizationExpiryClaim(ClaimsPrincipal user)
        {
            return user.FindFirst(ProjectClaimTypes.TOTPAuthorizationExpiry);
        }

        public static Claim GetAuthenticationTimeClaim(ClaimsPrincipal user)
        {
            return user.FindFirst(JwtClaimTypes.AuthenticationTime);
        }

        public static bool IsUserAuthorized(ClaimsPrincipal user)
        {
            // claim used to store expiry DateTime of TOTP authorization period
            Claim totpAuthorizationExpiryClaim = GetTOTPAuthorizationExpiryClaim(user);
            Claim authenticationTimeClaim = GetAuthenticationTimeClaim(user);

            // check if the current time is within the duration of the TOTPAuthorizationExpiry claim's value
            if (totpAuthorizationExpiryClaim != null)
            {
                DateTime authorizationExpiryDateTime = DateTime.Parse(totpAuthorizationExpiryClaim.Value);

                if (DateTime.UtcNow < authorizationExpiryDateTime)
                    return true;
            }

            // check if authentication was performed within the duration of the TOTPAuthorizationExpiry claim's value
            if (authenticationTimeClaim != null)
            {
                long unixAuthenticationTime = long.Parse(authenticationTimeClaim.Value);
                DateTime authenticationTime = DateTimeOffset.FromUnixTimeSeconds(unixAuthenticationTime).UtcDateTime;

                return DateTime.UtcNow < authenticationTime.AddSeconds(AccountOptions.TOTPAuthorizationDurationInSeconds);
            }

            return false;
        }
    }
}
