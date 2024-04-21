namespace CoreIdentityServer.Internals.Constants.Account
{
    public class AccountOptions
    {
        // boolean indicating whether to show the sign out prompt or not, when a user signs out
        public const bool ShowSignOutPrompt = true;

        // boolean used to determine if the user should be redirected to another page automatically
        // after signing out
        public const bool AutomaticRedirectAfterSignOut = false;
        
        // the amount of time in seconds as integer, indicating the duration of TOTP authorization
        // period for a user, after which they will be challenged with TOTP authorization
        public const int TOTPAuthorizationDurationInSeconds = 300;
        
        // the lifetime of TempData in seconds as integer, after which the TempData will be
        // rendered invalid
        public const int TempDataLifetimeInSeconds = 180;
    }
}
