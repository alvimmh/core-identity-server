using System;
using CoreIdentityServer.Internals.Constants.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterAuthentication
    {
        public static IServiceCollection AddProjectAuthentication(this IServiceCollection services)
        {
            // Duende Identity Server adds two cookie authentication schemes by default
            // one is the DefaultCookieAuthentication scheme, another is ExternalCookieAuthenticationScheme
            // the latter sets the sign in & sign out scheme to external scheme
            //
            // since external identity providers are not used, they are overriden below to use the application scheme
            //
            // to avoid any possible misconfiguration, all schemes are set to IdentityConstants.ApplicationScheme as default below
            //
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignOutScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultForbidScheme = IdentityConstants.ApplicationScheme;
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
            });

            services.Configure<SecurityStampValidatorOptions>(options => {
                options.ValidationInterval = TimeSpan.FromSeconds(0);
            });

            services.ConfigureApplicationCookie(options => {
                options.ReturnUrlParameter = "returnurl";
                options.LoginPath = "/access/authentication/signin";
                options.AccessDeniedPath = "/access/authentication/accessdenied";

                // configure authentication cookie options
                options.Cookie.Name = AuthenticationCookieOptions.Name;
                options.Cookie.Domain = AuthenticationCookieOptions.Domain;
                options.Cookie.Path = AuthenticationCookieOptions.Path;
                options.Cookie.HttpOnly = AuthenticationCookieOptions.HttpOnly;
                options.Cookie.IsEssential = AuthenticationCookieOptions.IsEssential;
                options.Cookie.SecurePolicy = AuthenticationCookieOptions.SecurePolicy;
                options.Cookie.SameSite = AuthenticationCookieOptions.SameSite;
                options.Cookie.MaxAge = AuthenticationCookieOptions.Duration;

                options.ExpireTimeSpan = AuthenticationCookieOptions.Duration;
                options.SlidingExpiration = false;
            });

            // Duende Identity Server overrides the cookie samesite attribut to 'None' in order to support
            // front-channel logout using iframes
            // 'src/AspNetIdentity/IdentityServerBuilderExtensions.cs' of Duende Identity Server repository
            //
            // ref: https://github.com/DuendeSoftware/IdentityServer/blob/main/src/AspNetIdentity/IdentityServerBuilderExtensions.cs
            //
            // so post configuring this cookie option to override it to 'Lax' again as this application uses
            // back-channel logout
            //
            services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options => {
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            return services;
        }
    }
}
