using System;

namespace CoreIdentityServer.Internals.Constants.Authentication
{
    public static class AuthenticationCookieOptions
    {
        public const string CookieName = "identity";
        public static TimeSpan CookieDuration = TimeSpan.FromSeconds(86400);
    }
}
