using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.Authorization.Requirements
{
    public class ClientCredentialsRequirement : IAuthorizationRequirement
    {
        public ClientCredentialsRequirement()
        {}
    }
}
