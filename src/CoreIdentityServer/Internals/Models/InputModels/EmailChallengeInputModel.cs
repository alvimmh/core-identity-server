using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Internals.Models.InputModels
{
    public class EmailChallengeInputModel
    {
        [Required]
        [StringLength(6)]
        public string VerificationCode { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string ResendEmailRecordId { get; set; }
    }
}
