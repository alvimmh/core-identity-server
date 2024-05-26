using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.AuthorizationPolicies.Requirements
{
    public class TOTPChallengeRequirement : IAuthorizationRequirement
    {
        public TOTPChallengeRequirement()
        {}
    }
}
