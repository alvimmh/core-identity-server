using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models.MFA
{
    public class SetEmailAuthenticationInputModel
    {
        [Required]
        public int Enable { get; set; }
    }
}
