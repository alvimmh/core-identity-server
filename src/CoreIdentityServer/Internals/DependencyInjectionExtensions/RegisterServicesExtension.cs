using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Areas.Enroll.Services;
using CoreIdentityServer.Internals.Services.EmailService;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterServicesExtension
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            // the ActionContextAccessor to gain access to the current action context
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // project's email service
            services.AddSingleton<EmailService>();

            services.AddScoped<SignUpService>();
            services.AddScoped<AuthenticationService>();

            return services;
        }
    }
}
