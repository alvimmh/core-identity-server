using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Vault.Models.Profile
{
    public class UserEmailInputModel
    {
        [Required]
        public string user_id { get; set; }

        [Required]
        public string client_id { get; set; }

        [Required]
        public string client_secret { get; set; }
    }
}