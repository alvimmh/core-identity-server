// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        // field to indicate if user has completed registration for Core Identity Server, i.e, email is confirmed & TOTP access is registered
        public bool AccountRegistered { get; set; }
    }
}
