// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer.Models;

namespace CoreIdentityServer.Areas.ClientServices.Models.Correspondence
{
    public class ErrorViewModel
    {
        public ErrorMessage Error { get; set; }

        public ErrorViewModel()
        {}

        public ErrorViewModel(string error)
        {
            Error = new ErrorMessage { Error = error };
        }
    }
}