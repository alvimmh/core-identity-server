// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using CoreIdentityServer.Internals.Models.ViewModels;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Internals.Extensions
{
    public static class Extensions
    {
        // Checks if the redirect URI is for a native client
        public static bool IsNativeClient(this AuthorizationRequest context)
        {
            return !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
               && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
        }

        // Configures the Loading Page action result and returns the specified (Redirect.cshtml) view
        public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri)
        {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = "";
            
            return controller.View(viewName, new RedirectViewModel { RedirectUrl = redirectUri });
        }
    }
}
