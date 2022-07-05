using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.Authorization.Requirements
{
    public class TAAClientCredentialsChallengeRequirement : IAuthorizationRequirement
    {
        public TAAClientCredentialsChallengeRequirement()
        {}
    }
}
