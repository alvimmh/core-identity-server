namespace CoreIdentityServer.Internals.Constants.Authorization
{
    public class Policies
    {
        // policy name for TOTP challenge authorization
        public const string TOTPChallenge = "TOTPChallenge";

        // policy name for client credentials authorization
        public const string ClientCredentials = "ClientCredentials";
        
        // policy name for administrative access authorization
        public const string AdministrativeAccess = "AdministrativeAccess";
    }
}