using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models.Authentication
{
    public class TOTPChallengeInputModel
    {
        [Required]
        [StringLength(6)]
        public string VerificationCode { get; set; }
    }
}
