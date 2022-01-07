using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Enroll.Models.SignUp
{
    public class ProspectiveUserInputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
    }
}