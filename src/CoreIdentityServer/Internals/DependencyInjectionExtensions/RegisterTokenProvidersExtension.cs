using System;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.TokenProviders.GenericTOTPTokenProvider;
using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Internals.DependencyInjectionExtensions
{
    public static class RegisterTokenProvidersExtension
    {
        public static IdentityBuilder AddProjectTokenProviders(this IdentityBuilder identityBuilder)
        {
            Type genericTOTPTokenProviderType = typeof(GenericTOTPTokenProvider<>).MakeGenericType(identityBuilder.UserType);

            return identityBuilder.AddTokenProvider(CustomTokenOptions.GenericTOTPTokenProvider, genericTOTPTokenProviderType);
        }
    }
}
