using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Internals.Models.InputModels
{
    public class BackChannelNotificationRequest
    {
        [Required]
        public string NotificationType { get; set; }

        [Required]
        public string JWTToken { get; set; }

        [Required]
        public string ClientId { get; set; }

        public string ClientNotificationUri { get; set; }
    }
}
