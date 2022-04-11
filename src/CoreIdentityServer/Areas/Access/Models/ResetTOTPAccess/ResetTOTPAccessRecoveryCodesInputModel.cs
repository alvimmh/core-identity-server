using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess
{
    public class ResetTOTPAccessRecoveryCodesInputModel
    {
        [Required]
        public string Id { get; set; }
    }
}