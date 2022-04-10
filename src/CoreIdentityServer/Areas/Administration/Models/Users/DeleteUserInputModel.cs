using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Administration.Models.Users
{
    public class DeleteUserInputModel
    {
        [Required]
        public string Id { get; set; }
    }
}
