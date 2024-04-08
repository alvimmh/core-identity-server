namespace CoreIdentityServer.Internals.Constants.UserActions
{
    public class UserActionContexts
    {
        // sign in email challenge is successful
        public const string SignInEmailChallenge = "SignInEmailChallenge";

        // sign in TOTP challenge is successful
        public const string SignInTOTPChallenge = "SignInTOTPChallenge";

        // TOTP access recovery challenge is successful
        public const string TOTPAccessRecoveryChallenge = "TOTPAccessRecoveryChallenge";

        // confirm email challenge is successful
        public const string ConfirmEmailChallenge = "ConfirmEmailChallenge";

        // dedicated TOTP challenge is successful
        public const string TOTPChallenge = "TOTPChallenge";
    }
}