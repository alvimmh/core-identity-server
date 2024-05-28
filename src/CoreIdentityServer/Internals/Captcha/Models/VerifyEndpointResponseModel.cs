using CoreIdentityServer.Internals.Captcha.Constants;
using Newtonsoft.Json;

namespace CoreIdentityServer.Internals.Captcha.Models
{
    // Model for the response received from the siteverify/ endpoint
    public class VerifyEndpointResponseModel
    {
        [JsonProperty(GeneralConstants.ResponseKeySuccess)]
        public bool Success { get; set; }
    }
}
