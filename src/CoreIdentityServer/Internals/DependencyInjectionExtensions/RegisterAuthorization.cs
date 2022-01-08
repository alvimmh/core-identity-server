using CoreIdentityServer.Internals.Authorization.Requirements;
using CoreIdentityServer.Internals.Constants.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterAuthorization
    {
        public static IServiceCollection AddProjectAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options => {
                options.AddPolicy(Policies.TOTPChallenge, policy => policy.AddRequirements(new TOTPChallengeRequirement()));
            });

            return services;
        }
    }
}
