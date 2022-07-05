using Microsoft.AspNetCore.Authorization;

namespace CoreIdentityServer.Internals.Authorization.Requirements
{
    public class AdministrativeAccessChallengeRequirement : IAuthorizationRequirement
    {
        public AdministrativeAccessChallengeRequirement()
        {}
    }
}
