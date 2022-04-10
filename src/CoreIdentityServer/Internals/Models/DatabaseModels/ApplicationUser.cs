// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Internals.Models.DatabaseModels
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        // field to indicate if user has completed registration for Core Identity Server, i.e, email is confirmed & TOTP access is registered
        public bool AccountRegistered { get; set; }

        // field to indicate if user needs to reset their authenticator
        public bool RequiresAuthenticatorReset { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSignedInAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        
        // field to indicate if user is blocked from using CIS or not
        public bool Blocked { get; private set; }

        // field to indicate if user is soft-deleted or not
        public bool Archived { get; private set; }

        public void UpdateLastSignedInTimeStamp()
        {
            DateTime currentDateTime = DateTime.UtcNow;

            LastSignedInAt = currentDateTime;
            UpdatedAt = currentDateTime;
        }

        public void SetBlock(bool block)
        {
            Blocked = block;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Archive()
        {
            Archived = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
