using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Areas.Administration.Services;
using CoreIdentityServer.Areas.ClientServices.Services;
using CoreIdentityServer.Areas.Enroll.Services;
using CoreIdentityServer.Areas.General.Services;
using CoreIdentityServer.Areas.Vault.Services;
using CoreIdentityServer.Internals.AuthorizationPolicies.Handlers;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.BackChannelCommunications;
using CoreIdentityServer.Internals.Services.Email;
using CoreIdentityServer.Internals.Services.Identity.IdentityService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterServices
    {
        // registers project services to the DI
        public static IServiceCollection AddProjectServices(
            this IServiceCollection services,
            IConfiguration configuration
        ) {
            // the HttpContextAccessor to gain access to the current HttpContext
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // the ActionContextAccessor to gain access to the current action context
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // project's email service
            services.AddSingleton<SMTPService>();

            // add authorization policy handlers
            services.AddSingleton<IAuthorizationHandler, TOTPChallengeHandler>();
            services.AddScoped<IAuthorizationHandler, AdministrativeAccessHandler>();
            services.AddScoped<IAuthorizationHandler, ClientCredentialsHandler>();

            services.AddSingleton<RouteEndpointService>();

            services.AddScoped<EmailService>();
            services.AddScoped<OIDCTokenService>();
            services.AddScoped<BackChannelNotificationService>();
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
            services.AddScoped<MFAService>();
            services.AddScoped<PagesService>();

            services.AddSessionStorage();

            services.AddCaptcha(configuration);

            return services;
        }
    }
}
