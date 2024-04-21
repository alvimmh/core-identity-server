using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.Authorization.Requirements
{
    public class AdministrativeAccessRequirement : IAuthorizationRequirement
    {
        public AdministrativeAccessRequirement()
        {}
    }
}
