using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Enroll.Models
{
    public class RegisterTOTPAccessInputModel
    {
        public string AuthenticatorKey { get; set; }
        public string AuthenticatorKeyUri { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6)]
        public string TOTPCode { get; set; }
    }
}