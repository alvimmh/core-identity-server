using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.AuthorizationPolicies.Requirements
{
    public class ClientCredentialsRequirement : IAuthorizationRequirement
    {
        public ClientCredentialsRequirement()
        {}
    }
}
