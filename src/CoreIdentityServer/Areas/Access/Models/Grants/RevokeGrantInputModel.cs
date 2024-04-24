// Copyright (c) Duende Software. All rights reserved.
//
// By accessing the Duende IdentityServer code here, you are agreeing to the following licensing terms:
// https://duendesoftware.com/license/identityserver.pdf
//
// or, alternatively you can view the license in the project's ~/Licenses/DuendeSoftwareLicense.pdf file
//
// If you do not agree to these terms, do not access the Duende IdentityServer code.


using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models.Grants
{
    public class RevokeGrantInputModel
    {
        [Required]
        public string ClientId { get; set; }
    }
}