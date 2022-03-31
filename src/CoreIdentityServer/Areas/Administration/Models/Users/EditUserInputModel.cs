using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Administration.Models.Users
{
    public class EditUserInputModel
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
    }
}
