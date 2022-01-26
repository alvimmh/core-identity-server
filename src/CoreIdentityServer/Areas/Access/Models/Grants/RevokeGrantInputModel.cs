using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models.Grants
{
    public class RevokeGrantInputModel
    {
        [Required]
        public string ClientId { get; set; }
    }
}