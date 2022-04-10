using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Internals.Models.InputModels
{
    public class CreateTokenInputModel
    {
        [Required]
        public string SubjectId { get; set; }

        [Required]
        public string ClientId { get; set; }

        public string SessionId { get; set; }
    }
}
