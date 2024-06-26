using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess
{
    public class InitiateTOTPAccessRecoveryChallengeInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
