namespace CoreIdentityServer.Internals.Constants.Tokens
{
    public class CustomTokenOptions
    {
        public const string GenericTOTPTokenProvider = "GenericTOTPTokenProvider";
        public const int DefaultTokenLifetimeInSeconds = 180;
        public const string BackChannelDeleteTokenPostBodyKey = "delete_token";
    }
}