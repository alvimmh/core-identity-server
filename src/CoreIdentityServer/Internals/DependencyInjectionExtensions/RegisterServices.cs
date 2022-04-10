using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Areas.Administration.Services;
using CoreIdentityServer.Areas.ClientServices.Services;
using CoreIdentityServer.Areas.Enroll.Services;
using CoreIdentityServer.Areas.Vault.Services;
using CoreIdentityServer.Internals.Authorization.Handlers;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterServices
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            // the ActionContextAccessor to gain access to the current action context
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // project's email service
            services.AddSingleton<SMTPService>();

            // add TOTPChallenge authorization policy handler
            services.AddSingleton<IAuthorizationHandler, TOTPChallengeHandler>();

            services.AddSingleton<RouteEndpointService>();

            services.AddScoped<EmailService>();
            services.AddScoped<OIDCTokenService>();
            services.AddScoped<IdentityService>();
            services.AddScoped<SignUpService>();
            services.AddScoped<AuthenticationService>();
            services.AddScoped<ResetTOTPAccessService>();
            services.AddScoped<ProfileService>();
            services.AddScoped<CorrespondenceService>();
            services.AddScoped<ConsentService>();
            services.AddScoped<GrantsService>();
            services.AddScoped<RolesService>();
            services.AddScoped<UsersService>();

            return services;
        }
    }
}
