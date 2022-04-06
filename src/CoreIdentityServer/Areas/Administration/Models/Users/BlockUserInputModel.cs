using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Administration.Models.Users
{
    public class BlockUserInputModel
    {
        [Required]
        public string Id { get; set; }
    }
}
