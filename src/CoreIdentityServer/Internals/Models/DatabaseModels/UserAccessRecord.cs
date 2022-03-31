using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreIdentityServer.Internals.Models.DatabaseModels
{
    public class UserAccessRecord
    {
        public string Id { get; private set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("Accessor")]
        public string AccessorId { get; set; }
        public virtual ApplicationUser Accessor { get; set; }

        public UserAccessRecord()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
