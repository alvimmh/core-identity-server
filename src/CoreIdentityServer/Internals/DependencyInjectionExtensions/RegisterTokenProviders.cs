using System;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.TokenProviders.GenericTOTPTokenProvider;
using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterTokenProviders
    {
        // registers the custom token provider for the application
        public static IdentityBuilder AddProjectTokenProviders(this IdentityBuilder identityBuilder)
        {
            Type genericTOTPTokenProviderType = typeof(GenericTOTPTokenProvider<>).MakeGenericType(identityBuilder.UserType);

            return identityBuilder.AddTokenProvider(CustomTokenOptions.GenericTOTPTokenProvider, genericTOTPTokenProviderType);
        }
    }
}
