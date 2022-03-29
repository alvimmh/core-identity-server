using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Administration.Models.Roles
{
    public class DeleteRoleInputModel
    {
        [Required]
        public string Id { get; set; }
    }
}