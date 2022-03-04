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
        public string SignInTimeStamps { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public void SetSignInTimeStamps()
        {
            DateTime currentDateTime = DateTime.UtcNow;
            string currentDateTimeString = currentDateTime.ToString();

            SignInTimeStamps = SignInTimeStamps == null ? currentDateTimeString : $"{SignInTimeStamps},{currentDateTimeString}";
            UpdatedAt = currentDateTime;
        }
    }
}
