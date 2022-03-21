// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using CoreIdentityServer.Internals.Constants.Authentication;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace CoreIdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope("teamadha_api", "Team Adha API")
            };

        public static IEnumerable<Client> Clients =>
            new List<Client>
            {
                // Team Adha backend interactive client using code flow + pkce
                new Client
                {
                    // basic settings
                    Enabled = true,
                    ClientId = "teamadha_backend",
                    RequireClientSecret = true,
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    AllowPlainTextPkce = false,
                    RedirectUris = { "{SetClientUriHere}/administration/authentication/signin_oidc" },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        "teamadha_api"
                    },
                    AllowOfflineAccess = true,

                    // authentication/session management
                    PostLogoutRedirectUris = { "{SetClientUriHere}" },
                    FrontChannelLogoutUri = null,
                    FrontChannelLogoutSessionRequired = false,
                    BackChannelLogoutUri = "{SetClientUriHere}/administration/authentication/signout_oidc",
                    BackChannelLogoutSessionRequired = true,
                    EnableLocalLogin = true,
                    IdentityProviderRestrictions = { "https://localhost:5001" },

                    // match this value with the authentication cookie lifetime
                    UserSsoLifetime = AuthenticationCookieOptions.CookieDuration.Seconds,

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
                    AbsoluteRefreshTokenLifetime = 604800,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Absolute,
                    UpdateAccessTokenClaimsOnRefresh = true,

                    // consent screen settings
                    RequireConsent = true,
                    AllowRememberConsent = true,
                    ConsentLifetime = 15552000,
                    ClientName = "Team Adha Administrative",
                    ClientUri = "{SetClientUriHere}",
                    LogoUri = "{SetClientUriHere}",
                }
            };
    }
}