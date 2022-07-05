using System.Threading.Tasks;
using CoreIdentityServer.Internals.Authorization.Requirements;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreIdentityServer.Internals.Authorization.Handlers
{
    public class TAAClientCredentialsChallengeHandler : AuthorizationHandler<TAAClientCredentialsChallengeRequirement>
    {
        private HttpContext HttpContext;
        private readonly IClientStore ClientStore;
        private readonly IOptions<IdentityServerOptions> IdentityOptions;

        public TAAClientCredentialsChallengeHandler(
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            IOptions<IdentityServerOptions> identityOptions
        ) {
            HttpContext = httpContextAccessor.HttpContext;
            ClientStore = clientStore;
            IdentityOptions = identityOptions;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext authorizationHandlerContext,
            TAAClientCredentialsChallengeRequirement requirement
        ) {
            PostBodySecretParser postBodySecretParser = new PostBodySecretParser(
                IdentityOptions.Value, new LoggerFactory().CreateLogger<PostBodySecretParser>()
            );

            ParsedSecret parsedClientSecret = await postBodySecretParser.ParseAsync(HttpContext);

            Client teamAdhaAdministrativeClient = await ClientStore.FindClientByIdAsync(parsedClientSecret.Id);

            if (teamAdhaAdministrativeClient != null)
            {
                HashedSharedSecretValidator hashedSharedSecretValidator = new HashedSharedSecretValidator(
                    new LoggerFactory().CreateLogger<HashedSharedSecretValidator>()
                );

                SecretValidationResult clientVerificationResult = await hashedSharedSecretValidator.ValidateAsync(
                    teamAdhaAdministrativeClient.ClientSecrets, parsedClientSecret
                );

                if (clientVerificationResult.Success)
                    authorizationHandlerContext.Succeed(requirement);
            }
        }
    }
}
