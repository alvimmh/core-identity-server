using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.AuthorizationPolicies.Requirements
{
    public class AdministrativeAccessRequirement : IAuthorizationRequirement
    {
        public AdministrativeAccessRequirement()
        {}
    }
}
