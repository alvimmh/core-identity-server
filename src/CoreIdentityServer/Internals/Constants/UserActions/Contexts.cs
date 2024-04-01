namespace CoreIdentityServer.Internals.Constants.UserActions
{
    public class UserActionContexts
    {
        // sign in email challenge is successful
        public const string SignInEmailChallenge = "SignInEmailChallenge";

        // sign in TOTP challenge is successful
        public const string SignInTOTPChallenge = "SignInTOTPChallenge";
        public const string ResetTOTPAccessRecoveryChallenge = "ResetTOTPAccessRecoveryChallenge";
        public const string ConfirmEmailChallenge = "ConfirmEmailChallenge";

        // dedicated TOTP challenge is successful
        public const string TOTPChallenge = "TOTPChallenge";
    }
}