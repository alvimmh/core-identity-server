using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterAuthentication
    {
        public static IServiceCollection AddProjectAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication();

            services.Configure<SecurityStampValidatorOptions>(options => {
                options.ValidationInterval = TimeSpan.FromSeconds(0);
            });

            services.ConfigureApplicationCookie(options => {
                options.LoginPath = "/Access/Authentication/SignIn";
                options.AccessDeniedPath = "/Access/Authentication/TOTPChallenge";
            });

            return services;
        }
    }
}
