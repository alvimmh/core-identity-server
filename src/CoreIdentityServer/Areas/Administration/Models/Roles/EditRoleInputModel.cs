using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Administration.Models.Roles
{
    public class EditRoleInputModel
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
