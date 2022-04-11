using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Enroll.Models.SignUp
{
    public class RegisterTOTPAccessInputModel
    {
        [Required]
        public string AuthenticatorKey { get; set; }

        [Required]
        public string AuthenticatorKeyUri { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6)]
        public string TOTPCode { get; set; }

        [Required]
        [StringLength(6)]
        public string SessionVerificationTOTPCode { get; set; }
    }
}