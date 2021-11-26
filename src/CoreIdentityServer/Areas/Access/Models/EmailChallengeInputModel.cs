using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models
{
    public class EmailChallengeInputModel
    {
        [Required]
        [StringLength(6)]
        public string VerificationCode { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
