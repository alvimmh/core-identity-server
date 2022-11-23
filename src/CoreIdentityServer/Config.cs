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
        private static string Environment = Startup.StaticConfiguration["environment"];




        // change it to localhost for development environment
        private const string DevelopmentDomain = "localhost";
        private const string ProductionDomain = "bonicinitiatives.biz";

        public static string GetApplicationDomain()
        {
            return Environment == "Development" ? DevelopmentDomain : ProductionDomain;
        }




        // change it to local url https://localhost:5001 for development environment
        private const string DevelopmentUrl = "https://localhost:5001";
        private const string ProductionUrl = "https://bonicinitiatives.biz";

        public static string GetApplicationUrl()
        {
            return Environment == "Development" ? DevelopmentUrl : ProductionUrl;
        }




        // change TeamadhaBackendClientDevelopmentUrl to local url for development environment
        private const string TeamadhaBackendClientDevelopmentUrl = "";
        private const string TeamadhaBackendClientProductionUrl = "https://administrative.teamadha.com";

        public static string GetTeamadhaBackendClientUrl()
        {
            return Environment == "Development" ? TeamadhaBackendClientDevelopmentUrl : TeamadhaBackendClientProductionUrl;
        }
        



        // change TeamadhaFrontendClientDevelopmentUrl to local url for development environment
        private const string TeamadhaFrontendClientDevelopmentUrl = "https://localhost:3000";
        private const string TeamadhaFrontendClientProductionUrl = "https://teamadha.com";

        public static string GetTeamadhaFrontendClientUrl()
        {
            return Environment == "Development" ? TeamadhaFrontendClientDevelopmentUrl : TeamadhaFrontendClientProductionUrl;
        }

        private static string TeamadhaFrontendClientRedirectUrl = $"{Config.GetTeamadhaFrontendClientUrl()}?idp-redirect=true";




        // set them to the appsettings.environment.json files
        private static string TeamadhaBackendClientSecret = Startup.StaticConfiguration["teamadha_backend_client_secret"];
        private static string TeamadhaFrontendClientSecret = Startup.StaticConfiguration["teamadha_frontend_client_secret"];




        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Email()
            };




        public static IEnumerable<ApiScope> ApiScopes =>
            new List<ApiScope>
            {
                new ApiScope("administrative_access", "Administrative Access"),
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
                    ClientSecrets = { new Secret(TeamadhaBackendClientSecret.Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    AllowPlainTextPkce = false,
                    RedirectUris = { $"{Config.GetTeamadhaBackendClientUrl()}/administration/authentication/signin_oidc" },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "administrative_access",
                        "teamadha_api"
                    },
                    AllowOfflineAccess = true,

                    // authentication/session management
                    PostLogoutRedirectUris = { Config.GetTeamadhaBackendClientUrl() },
                    FrontChannelLogoutUri = null,
                    FrontChannelLogoutSessionRequired = false,
                    BackChannelLogoutUri = $"{Config.GetTeamadhaBackendClientUrl()}/administration/authentication/signout_oidc",
                    BackChannelLogoutSessionRequired = true,
                    EnableLocalLogin = true,
                    IdentityProviderRestrictions = { Config.GetApplicationUrl() },

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
                    ClientUri = Config.GetTeamadhaBackendClientUrl(),
                    LogoUri = Config.GetTeamadhaBackendClientUrl(),
                },



                
                // Team Adha interactive client using code flow + pkce
                new Client
                {
                    // basic settings
                    Enabled = true,
                    ClientId = "teamadha_frontend",
                    RequireClientSecret = true,
                    ClientSecrets = { new Secret(TeamadhaFrontendClientSecret.Sha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    AllowPlainTextPkce = false,
                    RedirectUris = { $"{Config.GetTeamadhaFrontendClientUrl()}/signin-oidc" },
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "teamadha_api"
                    },
                    AllowOfflineAccess = true,

                    // authentication/session management
                    PostLogoutRedirectUris = { TeamadhaFrontendClientRedirectUrl },
                    FrontChannelLogoutUri = null,
                    FrontChannelLogoutSessionRequired = false,
                    BackChannelLogoutUri = $"{Config.GetTeamadhaBackendClientUrl()}/api/v1/authentication/signout_oidc",
                    BackChannelLogoutSessionRequired = true,
                    EnableLocalLogin = true,
                    IdentityProviderRestrictions = { Config.GetApplicationUrl() },

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
                    ClientName = "Team Adha",
                    ClientUri = Config.GetTeamadhaFrontendClientUrl(),
                    LogoUri = Config.GetTeamadhaFrontendClientUrl(),
                }
            };
    }
}