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
        /// <summary>
        ///     protected override Task HandleRequirementAsync(
        ///         AuthorizationHandlerContext authorizationHandlerContext,
        ///         TOTPChallengeRequirement requirement
        ///     )
        ///     
        ///     This authorization handler is used to authorize users by checking if
        ///     they can access endpoints or resources requiring a mandatory
        ///     TOTP authenticator based authorization challenge.
        /// </summary>
        /// <param name="authorizationHandlerContext">Context for the handler</param>
        /// <param name="requirement">The requirement for the authorization handler</param>
        /// <returns>void</returns>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext authorizationHandlerContext,
            TOTPChallengeRequirement requirement
        ) {
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


        /// <summary>
        ///     public static Claim GetTOTPAuthorizationExpiryClaim(ClaimsPrincipal user)
        /// 
        ///     Gets the ProjectClaimTypes.TOTPAuthorizationExpiry claim from the user's claims.
        /// </summary>
        /// <param name="user">The ClaimsPrincipal object</param>
        /// <returns>The ProjectClaimTypes.TOTPAuthorizationExpiry claim</returns>
        public static Claim GetTOTPAuthorizationExpiryClaim(ClaimsPrincipal user)
        {
            return user.FindFirst(ProjectClaimTypes.TOTPAuthorizationExpiry);
        }


        /// <summary>
        ///     public static Claim GetAuthenticationTimeClaim(ClaimsPrincipal user)
        /// 
        ///     Gets the ProjectClaimTypes.TOTPAuthorizationExpiry claim from the user's claims.
        /// </summary>
        /// <param name="user">The ClaimsPrincipal object</param>
        /// <returns>The JwtClaimTypes.AuthenticationTime claim</returns>
        public static Claim GetAuthenticationTimeClaim(ClaimsPrincipal user)
        {
            return user.FindFirst(JwtClaimTypes.AuthenticationTime);
        }

        /// <summary>
        ///     public static bool IsUserAuthorized(ClaimsPrincipal user)
        ///     
        ///     Checks if the user has access to endpoints or resources requiring a mandatory
        ///         TOTP authenticator based authorization challenge. 
        ///     
        ///     1. Gets the ProjectClaimTypes.TOTPAuthorizationExpiry claim from the user's claims.
        ///     
        ///     2. Gets the JwtClaimTypes.AuthenticationTime claim from the user's claims.
        ///     
        ///     3. If the ProjectClaimTypes.TOTPAuthorizationExpiry claim is not empty, the claim's value is
        ///         parsed as a DateTime object and checked if the DateTime is greater than the DateTime.UtcNow.
        ///             If it is, it means the user is still in their TOTP authorization period and the function
        ///                 returns true. If not, the code continues.
        ///             
        ///     4. If the JwtClaimTypes.AuthenticationTime claim is not empty, the claims value is parsed to a
        ///         DateTime object and checked if the DateTime is greater than DateTime.UtcNow by AccountOptions.
        ///             TOTPAuthorizationDurationInSeconds seconds. If it is, then the user was authenticated not
        ///                 too long ago and doesn't require a TOTP challenge, and the function returns true. If not,
        ///                     the function ultimately returns false meaning the user requires a TOTP challenge.
        /// </summary>
        /// <param name="user">The ClaimsPrincipal object (user)</param>
        /// <returns>Boolean indicating the user's TOTP authorization status.</returns>
        public static bool IsUserAuthorized(ClaimsPrincipal user)
        {
            // claim used to store expiry DateTime of TOTP authorization period
            Claim totpAuthorizationExpiryClaim = GetTOTPAuthorizationExpiryClaim(user);

            // claim used to store the last authentication time of the user
            Claim authenticationTimeClaim = GetAuthenticationTimeClaim(user);

            // check if the current time is within the duration of the TOTPAuthorizationExpiry claim
            if (totpAuthorizationExpiryClaim != null)
            {
                DateTime authorizationExpiryDateTime = DateTime.Parse(totpAuthorizationExpiryClaim.Value);

                if (DateTime.UtcNow < authorizationExpiryDateTime)
                    return true;
            }

            // check if authentication was performed within the duration of AccountOptions.TOTPAuthorizationDurationInSeconds
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
