using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models
{
    public class SignInInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6)]
        public string TOTPCode { get; set; }
    }
}
