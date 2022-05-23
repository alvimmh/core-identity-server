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
        // change it to localhost for development environment
        public const string CISApplicationDomain = "bonicinitiatives.biz";

        // change it to local url https://localhost:5001 for development environment
        public const string CISApplicationUrl = "https://bonicinitiatives.biz";

        // change it to local url for development environment
        public const string TeamadhaBackendClientUrl = "https://{url_for_team_adha_administrative}";

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
                // Team Adha Administrative interactive client using code flow + pkce
                new Client
                {
                    // basic settings
                    Enabled = true,
                    ClientId = "teamadha_administrative",
                    RequireClientSecret = true,
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    AllowPlainTextPkce = false,
                    RedirectUris = { $"{TeamadhaBackendClientUrl}/administration/authentication/signin_oidc" },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "teamadha_api"
                    },
                    AllowOfflineAccess = true,

                    // authentication/session management
                    PostLogoutRedirectUris = { TeamadhaBackendClientUrl },
                    FrontChannelLogoutUri = null,
                    FrontChannelLogoutSessionRequired = false,
                    BackChannelLogoutUri = $"{TeamadhaBackendClientUrl}/administration/authentication/signout_oidc",
                    BackChannelLogoutSessionRequired = true,
                    EnableLocalLogin = true,
                    IdentityProviderRestrictions = { CISApplicationUrl },

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
                    ClientName = "Team Adha Administrative",
                    ClientUri = TeamadhaBackendClientUrl,
                    LogoUri = TeamadhaBackendClientUrl,
                }
            };
    }
}