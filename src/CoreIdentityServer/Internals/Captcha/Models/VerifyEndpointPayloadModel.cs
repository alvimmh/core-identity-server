using CoreIdentityServer.Internals.Captcha.Constants;
using Newtonsoft.Json;

namespace CoreIdentityServer.Internals.Captcha.Models
{
    // Model for the payload sent to the siteverify/ endpoint
    public class VerifyEndpointPayloadModel
    {
        [JsonProperty(GeneralConstants.PayloadKeySecret)]
        public string Secret { get; set; }

        [JsonProperty(GeneralConstants.PayloadKeyResponse)]
        public string Response { get; set; }

        [JsonProperty(GeneralConstants.PayloadKeyRemoteIP)]
        public string RemoteIP { get; set; }
    }
}
