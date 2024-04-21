using System;
using CoreIdentityServer.Internals.Constants.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterSessionStorage
    {
        // adds and configures services for the application session storage
        public static IServiceCollection AddSessionStorage(this IServiceCollection services)
        {
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(5);

                options.Cookie.Name = SessionCookieOptions.Name;
                options.Cookie.Domain = SessionCookieOptions.Domain;
                options.Cookie.Path = SessionCookieOptions.Path;
                options.Cookie.HttpOnly = SessionCookieOptions.HttpOnly;
                options.Cookie.IsEssential = SessionCookieOptions.IsEssential;
                options.Cookie.SecurePolicy = SessionCookieOptions.SecurePolicy;
                // samesite set to None to allow cross-site requests
                options.Cookie.SameSite = SessionCookieOptions.SameSite;
                options.Cookie.MaxAge = SessionCookieOptions.Duration;
            });

            return services;
        }
    }
}
