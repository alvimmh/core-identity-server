using CoreIdentityServer.Internals.AuthorizationPolicies.Requirements;
using CoreIdentityServer.Internals.Constants.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterAuthorization
    {
        // adds authorization policies for the application
        public static IServiceCollection AddProjectAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options => {
                options.AddPolicy(
                    Policies.TOTPChallenge,
                    policy => policy.AddRequirements(
                        new TOTPChallengeRequirement()
                    )
                );
            });

            services.AddAuthorization(options => {
                options.AddPolicy(
                    Policies.AdministrativeAccess,
                    policy => policy.AddRequirements(
                        new AdministrativeAccessRequirement()
                    )
                );
            });

            services.AddAuthorization(options => {
                options.AddPolicy(
                    Policies.ClientCredentials,
                    policy => policy.AddRequirements(
                        new ClientCredentialsRequirement()
                    )
                );
            });

            return services;
        }
    }
}
