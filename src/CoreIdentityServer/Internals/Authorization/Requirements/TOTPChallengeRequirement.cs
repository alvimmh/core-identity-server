using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.Authorization.Requirements
{
    public class TOTPChallengeRequirement : IAuthorizationRequirement
    {
        public TOTPChallengeRequirement()
        {}
    }
}
