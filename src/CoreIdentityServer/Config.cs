// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using CoreIdentityServer.Internals.Constants.Storage;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace CoreIdentityServer
{
    public static class Config
    {
        // set values to Startup.StaticConfiguration in the appsettings.[environment].json files

        private static string ApplicationDomain = Startup.StaticConfiguration["application_domain"];
        private static string ApplicationUrl = Startup.StaticConfiguration["application_url"];

        private static string Client1Id = Startup.StaticConfiguration["client1_id"];
        private static string Client1Name = Startup.StaticConfiguration["client1_name"];
        private static string Client1Secret = Startup.StaticConfiguration["client1_secret"];
        private static string Client1Url = Startup.StaticConfiguration["client1_url"];
        
        private static string Client2Id = Startup.StaticConfiguration["client2_id"];
        private static string Client2Name = Startup.StaticConfiguration["client2_name"];
        private static string Client2Secret = Startup.StaticConfiguration["client2_secret"];
        private static string Client2Url = Startup.StaticConfiguration["client2_url"];
        private static string Client2RedirectUrl = $"{Client2Url}?idp-redirect=true";

        public static string GetApplicationDomain()
        {
            return ApplicationDomain;
        }

        public static string GetApplicationUrl()
        {
            return ApplicationUrl;
        }

        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope("client1_administrative_access", "Administrative Access"),
                new ApiScope("client1_resource_api", "Resource API")
            };

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                // client 1 - interactive client using code flow + pkce
                new Client
                {
                    // basic settings
                    Enabled = true,
                    ClientId = Client1Id,
                    RequireClientSecret = true,
                    ClientSecrets = { new Secret(Client1Secret.Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    AllowPlainTextPkce = false,
                    RedirectUris = { $"{Client1Url}/administration/authentication/signin_oidc" },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "client1_administrative_access",
                        "client1_resource_api"
                    },
                    AllowOfflineAccess = true,

                    // authentication/session management
                    PostLogoutRedirectUris = { Client1Url },
                    FrontChannelLogoutUri = null,
                    FrontChannelLogoutSessionRequired = false,
                    BackChannelLogoutUri = $"{Client1Url}/administration/authentication/signout_oidc",
                    BackChannelLogoutSessionRequired = true,
                    EnableLocalLogin = true,
                    IdentityProviderRestrictions = { ApplicationUrl },

                    // match this value with the authentication cookie lifetime
                    UserSsoLifetime = (int)AuthenticationCookieOptions.Duration.TotalSeconds,

                    // token settings
                    IdentityTokenLifetime = 180,
                    AccessTokenLifetime = 900,
                    AuthorizationCodeLifetime = 180,
                    AccessTokenType = AccessTokenType.Jwt,
                    IncludeJwtId = false,
                    Claims = null,
                    AlwaysSendClientClaims = false,
                    AlwaysIncludeUserClaimsInIdToken = false,

                    // refresh token settings
                    AbsoluteRefreshTokenLifetime = 3600,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,

                    // consent screen settings
                    RequireConsent = true,
                    AllowRememberConsent = true,
                    ConsentLifetime = 15552000,
                    ClientName = Client1Name,
                    ClientUri = Client1Url,
                    LogoUri = Client1Url,
                    CoordinateLifetimeWithUserSession = false,
                },



                
                // client 2 - interactive client using code flow + pkce
                new Client
                {
                    // basic settings
                    Enabled = true,
                    ClientId = Client2Id,
                    RequireClientSecret = true,
                    ClientSecrets = { new Secret(Client2Secret.Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    AllowPlainTextPkce = false,
                    RedirectUris = { $"{Client2Url}/signin-oidc" },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "client1_resource_api"
                    },
                    AllowOfflineAccess = true,

                    // authentication/session management
                    PostLogoutRedirectUris = { Client2RedirectUrl },
                    FrontChannelLogoutUri = null,
                    FrontChannelLogoutSessionRequired = false,
                    BackChannelLogoutUri = $"{Client1Url}/api/v1/authentication/signout_oidc",
                    BackChannelLogoutSessionRequired = true,
                    EnableLocalLogin = true,
                    IdentityProviderRestrictions = { ApplicationUrl },

                    // match this value with the authentication cookie lifetime
                    UserSsoLifetime = (int)AuthenticationCookieOptions.Duration.TotalSeconds,

                    // token settings
                    IdentityTokenLifetime = 180,
                    AccessTokenLifetime = 900,
                    AuthorizationCodeLifetime = 180,
                    AccessTokenType = AccessTokenType.Jwt,
                    IncludeJwtId = false,
                    Claims = null,
                    AlwaysSendClientClaims = false,
                    AlwaysIncludeUserClaimsInIdToken = false,

                    // refresh token settings
                    AbsoluteRefreshTokenLifetime = 3600,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    UpdateAccessTokenClaimsOnRefresh = true,

                    // consent screen settings
                    RequireConsent = true,
                    AllowRememberConsent = true,
                    ConsentLifetime = 15552000,
                    ClientName = Client2Name,
                    ClientUri = Client2Url,
                    LogoUri = Client2Url,
                    CoordinateLifetimeWithUserSession = false,
                }
            };
    }
}