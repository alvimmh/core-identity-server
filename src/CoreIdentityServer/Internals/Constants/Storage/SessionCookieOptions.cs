using System;
using Microsoft.AspNetCore.Http;

namespace CoreIdentityServer.Internals.Constants.Storage
{
    // options for the session cookie
    public static class SessionCookieOptions
    {
        public const string Name = "session";
        public static string Domain = Config.GetApplicationDomain();
        public const string Path = "/";
        public const bool HttpOnly = true;
        public const bool IsEssential = true;
        public const CookieSecurePolicy SecurePolicy = CookieSecurePolicy.Always;

        // samesite set to Lax to allow cross-site requests
        public const SameSiteMode SameSite = SameSiteMode.Lax;

        public static TimeSpan Duration = TimeSpan.FromSeconds(1200);
    }
}
