using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.Authorization.Requirements
{
    public class ClientCredentialsChallengeRequirement : IAuthorizationRequirement
    {
        public ClientCredentialsChallengeRequirement()
        {}
    }
}
