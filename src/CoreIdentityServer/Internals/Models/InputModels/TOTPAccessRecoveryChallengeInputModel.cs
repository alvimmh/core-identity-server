using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Internals.Models.InputModels
{
    public class TOTPAccessRecoveryChallengeInputModel
    {
        [Required]
        [StringLength(8)]
        public string VerificationCode { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string ReturnUrl { get; set; }
    }
}
