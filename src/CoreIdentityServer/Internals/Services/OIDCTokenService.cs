using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Constants.Tokens;
using CoreIdentityServer.Internals.Models.InputModels;
using Duende.IdentityServer;
using IdentityModel;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Internals.Services
{
    public class OIDCTokenService : IDisposable
    {
        private IConfiguration Config;
        private IdentityServerTools Tools { get; set; }
        public bool ResourcesDisposed;

        public OIDCTokenService(
            IConfiguration config,
            IdentityServerTools tools
        ) {
            Config = config;
            Tools = tools;
        }

        // create JWT token
        public async Task<string> CreateTokenAsync(CreateTokenInputModel inputModel, string tokenEvent)
        {
            IEnumerable<Claim> claims = await CreateClaimsForTokenAsync(inputModel, tokenEvent);

            if (claims.Any(x => x.Type == JwtClaimTypes.Nonce))
            {
                throw new InvalidOperationException("The 'nonce' claim is not allowed in custom OIDC tokens.");
            }

            return await Tools.IssueJwtAsync(CustomTokenOptions.DefaultTokenLifetimeInSeconds, claims);
        }

        // Create claims for the token.
        protected Task<IEnumerable<Claim>> CreateClaimsForTokenAsync(CreateTokenInputModel inputModel, string tokenEvent)
        {
            string eventJSON = "{\"" + tokenEvent + "\":{} }";

            List<Claim> claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, inputModel.SubjectId),
                new Claim(JwtClaimTypes.Audience, inputModel.ClientId),
                new Claim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex)),
                new Claim(JwtClaimTypes.Events, eventJSON, IdentityServerConstants.ClaimValueTypes.Json)
            };

            if (inputModel.SessionId != null)
            {
                claims.Add(new Claim(JwtClaimTypes.SessionId, inputModel.SessionId));
            }

            return Task.FromResult(claims.AsEnumerable());
        }

        // clean up
        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            Tools = null;
            Config = null;
            ResourcesDisposed = true;
        }
    }
}
