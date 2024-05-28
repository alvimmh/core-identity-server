namespace CoreIdentityServer.Internals.Captcha.Constants
{
    public static class GeneralConstants
    {
        // name of form data for the captcha response
        public const string CaptchaResponseFormDataName = "cf-turnstile-response";

        // verify endpoint payload JSON keys
        public const string PayloadKeySecret = "secret";
        public const string PayloadKeyResponse = "response";
        public const string PayloadKeyRemoteIP = "remoteip";
        
        // verify endpoint response JSON keys
        public const string ResponseKeySuccess = "success";
    }
}
