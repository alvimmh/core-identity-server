using System;

namespace CoreIdentityServer.Areas.Administration.Models.Users
{
    public class UserDetailsViewModel : UserViewModel
    {
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; }
        public bool EmailConfirmed { get; set; }
        public int AccessFailedCount { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool RequiresAuthenticatorReset { get; set; }
        public bool Blocked { get; set; }
        public DateTime? LastSignedInAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
