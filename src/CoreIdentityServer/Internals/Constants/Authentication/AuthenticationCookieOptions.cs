using System;
using Microsoft.AspNetCore.Http;

namespace CoreIdentityServer.Internals.Constants.Authentication
{
    public static class AuthenticationCookieOptions
    {
        public const string Name = "identity";
        public static string Domain = Config.GetApplicationDomain();
        public const string Path = "/";
        public const bool HttpOnly = true;
        public const bool IsEssential = true;
        public const CookieSecurePolicy SecurePolicy = CookieSecurePolicy.Always;

        // samesite set to Lax to allow cross-site requests
        public const SameSiteMode SameSite = SameSiteMode.Lax;

        public static TimeSpan Duration = TimeSpan.FromSeconds(86400);
    }
}
