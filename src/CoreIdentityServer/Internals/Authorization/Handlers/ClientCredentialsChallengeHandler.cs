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
    public class ClientCredentialsChallengeHandler : AuthorizationHandler<ClientCredentialsChallengeRequirement>
    {
        private HttpContext HttpContext;
        private readonly IClientStore ClientStore;
        private readonly IOptions<IdentityServerOptions> IdentityOptions;

        public ClientCredentialsChallengeHandler(
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            IOptions<IdentityServerOptions> identityOptions
        ) {
            HttpContext = httpContextAccessor.HttpContext;
            ClientStore = clientStore;
            IdentityOptions = identityOptions;
        }


        /// <summary>
        ///     HandleRequirementAsync(
        ///         AuthorizationHandlerContext authorizationHandlerContext,
        ///         ClientCredentialsChallengeRequirement requirement
        ///     );
        ///     
        ///     This authorization handler is used to authorize clients of the identity server by matching client credentials
        ///     found in the post body against clients in the client store.
        ///     
        ///     The post body is supposed to come from a backend client.
        ///     
        ///     1. Parses the postBody using the PostBodySecretParser.
        ///     
        ///     2. Gets the client from the client store using the id found in the post body.
        ///     
        ///     3. Validates the client by matching the credentials from the post body.
        /// </summary>
        /// <param name="authorizationHandlerContext"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        /// 
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext authorizationHandlerContext,
            ClientCredentialsChallengeRequirement requirement
        ) {
            PostBodySecretParser postBodySecretParser = new PostBodySecretParser(
                IdentityOptions.Value, new LoggerFactory().CreateLogger<PostBodySecretParser>()
            );

            ParsedSecret parsedClientSecret = await postBodySecretParser.ParseAsync(HttpContext);

            Client client = await ClientStore.FindClientByIdAsync(parsedClientSecret.Id);

            if (client != null)
            {
                HashedSharedSecretValidator hashedSharedSecretValidator = new HashedSharedSecretValidator(
                    new LoggerFactory().CreateLogger<HashedSharedSecretValidator>()
                );

                SecretValidationResult clientVerificationResult = await hashedSharedSecretValidator.ValidateAsync(
                    client.ClientSecrets, parsedClientSecret
                );

                if (clientVerificationResult.Success)
                    authorizationHandlerContext.Succeed(requirement);
            }
        }
    }
}
