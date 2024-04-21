using System;
using Microsoft.EntityFrameworkCore;

namespace CoreIdentityServer.Internals.Models.DatabaseModels
{
    // Class representing records of user data access by administrative users
    [Index(nameof(UserId)), Index(nameof(AccessorId))]
    public class UserAccessRecord
    {
        public string Id { get; private set; }
        public string Purpose { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UserId { get; set; }
        public string AccessorId { get; set; }

        public UserAccessRecord()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
