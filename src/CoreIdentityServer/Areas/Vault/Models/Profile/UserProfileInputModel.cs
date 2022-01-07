using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Vault.Models.Profile
{
    public class UserProfileInputModel
    {
        [EmailAddress]
        public string Email { get; private set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public void SetEmail(string email)
        {
            Email = email;
        }
    }
}