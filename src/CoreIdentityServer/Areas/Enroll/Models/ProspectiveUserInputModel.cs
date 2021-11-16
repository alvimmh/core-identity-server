using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Enroll.Models
{
    public class ProspectiveUserInputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}