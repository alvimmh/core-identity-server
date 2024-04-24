// Copyright (c) Duende Software. All rights reserved.
//
// By accessing the Duende IdentityServer code here, you are agreeing to the following licensing terms:
// https://duendesoftware.com/license/identityserver.pdf
//
// or, alternatively you can view the license in the project's ~/Licenses/DuendeSoftwareLicense.pdf file
//
// If you do not agree to these terms, do not access the Duende IdentityServer code.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Constants.Authorization;
using CoreIdentityServer.Internals.Models.InputModels;
using Duende.IdentityServer;
using IdentityModel;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Internals.Services.BackChannelCommunications
{
    // Class to facilitate creation of OIDC standard JWT tokens
    public class OIDCTokenService : IDisposable
    {
        private IdentityServerTools Tools { get; set; }
        public bool ResourcesDisposed;

        public OIDCTokenService(IdentityServerTools tools) {
            Tools = tools;
        }

        // Creates a JWT token
        public async Task<string> CreateTokenAsync(CreateTokenInputModel inputModel, string tokenEvent)
        {
            IEnumerable<Claim> claims = await CreateClaimsForTokenAsync(inputModel, tokenEvent);

            if (claims.Any(x => x.Type == JwtClaimTypes.Nonce))
            {
                throw new InvalidOperationException("The 'nonce' claim is not allowed in custom OIDC tokens.");
            }

            return await Tools.IssueJwtAsync(CustomTokenOptions.DefaultTokenLifetimeInSeconds, claims);
        }

        // Creates claims for the token
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
            ResourcesDisposed = true;
        }
    }
}
