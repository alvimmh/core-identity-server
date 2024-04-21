// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Internals.Models.DatabaseModels
{
    // Class representing the users of the application
    public class ApplicationUser : IdentityUser
    {
        // Field to indicate if user has completed registration for Core Identity Server, i.e, email is confirmed & TOTP access is registered
        public bool AccountRegistered { get; set; }

        // Field to indicate if user needs to reset their authenticator
        public bool RequiresAuthenticatorReset { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSignedInAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        
        // Field to indicate if user is blocked from the application
        public bool Blocked { get; private set; }

        // Field to indicate if user is soft-deleted or not
        public bool Archived { get; private set; }

        // Updates the LastSignedInAt property of the user.
        public void UpdateLastSignedInTimeStamp()
        {
            DateTime currentDateTime = DateTime.UtcNow;

            LastSignedInAt = currentDateTime;
            UpdatedAt = currentDateTime;
        }

        // Sets the block status for the user
        public void SetBlock(bool block)
        {
            Blocked = block;
            UpdatedAt = DateTime.UtcNow;
        }

        // Soft-deletes the user
        public void Archive()
        {
            Archived = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
