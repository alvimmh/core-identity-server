using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Administration.Models.Roles
{
    public class CreateRoleInputModel
    {
        [Required]
        public string Name { get; set; }
    }
}
