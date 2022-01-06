using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Enroll.Models.SignUp
{
    public class ProspectiveUserInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}