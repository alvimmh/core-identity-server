namespace CoreIdentityServer.Internals.Constants.Authorization
{
    // options for custom tokens in this application
    public class CustomTokenOptions
    {
        public const string GenericTOTPTokenProvider = "GenericTOTPTokenProvider";
        public const int DefaultTokenLifetimeInSeconds = 180;
        public const string BackChannelDeleteTokenPostBodyKey = "delete_token";
    }
}