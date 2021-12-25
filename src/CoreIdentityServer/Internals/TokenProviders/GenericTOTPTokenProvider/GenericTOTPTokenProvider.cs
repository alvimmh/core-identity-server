/// Ref: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/TotpSecurityStampBasedTokenProvider.cs
/// Ref: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/EmailTokenProvider.cs

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Internals.TokenProviders.GenericTOTPTokenProvider
{
    public class GenericTOTPTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
    {
        /// <summary>
        /// Checks if a two-factor authentication token can be generated for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve the <paramref name="user"/> from.</param>
        /// <param name="user">The <typeparamref name="TUser"/> to check for the possibility of generating a two-factor authentication token.</param>
        /// <returns>True if the user has an email address set and confirmed, otherwise false.</returns>
        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> userManager, TUser user)
        {
            string userEmail = await userManager.GetEmailAsync(user);

            return !string.IsNullOrWhiteSpace(userEmail) && await userManager.IsEmailConfirmedAsync(user);
        }

        /// <summary>
        /// Generates a token for the specified <paramref name="user"/> and <paramref name="purpose"/>.
        /// </summary>
        /// <param name="purpose">The purpose the token will be used for.</param>
        /// <param name="userManager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
        /// <param name="user">The user a token should be generated for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the token for the specified 
        /// <paramref name="user"/> and <paramref name="purpose"/>.
        /// </returns>
        /// <remarks>
        /// The <paramref name="purpose"/> parameter allows a token generator to be used for multiple types of token whilst
        /// insuring a token for one purpose cannot be used for another. For example if you specified a purpose of "Email" 
        /// and validated it with the same purpose a token with the purpose of TOTP would not pass the check even if it was
        /// for the same user.
        /// 
        /// Implementations of <see cref="IUserTwoFactorTokenProvider{TUser}"/> should validate that purpose is not null or empty to
        /// help with token separation.
        /// </remarks>
        public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> userManager, TUser user)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }

            byte[] securityToken = await userManager.CreateSecurityTokenAsync(user);
            string modifier = await GetUserModifierAsync(purpose, userManager, user);

            return RFC6238AuthenticationService.GenerateCode(securityToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a flag indicating whether the specified <paramref name="token"/> is valid for the given
        /// <paramref name="user"/> and <paramref name="purpose"/>.
        /// </summary>
        /// <param name="purpose">The purpose the token will be used for.</param>
        /// <param name="token">The token to validate.</param>
        /// <param name="userManager">The <see cref="UserManager{TUser}"/> that can be used to retrieve user properties.</param>
        /// <param name="user">The user a token should be validated for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the a flag indicating the result
        /// of validating the <paramref name="token"> for the specified </paramref><paramref name="user"/> and <paramref name="purpose"/>.
        /// The task will return true if the token is valid, otherwise false.
        /// </returns>
        public override async Task<bool> ValidateAsync(string purpose, string inputTOTPCode, UserManager<TUser> userManager, TUser user)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }

            int code;
            if (!int.TryParse(inputTOTPCode, out code))
            {
                return false;
            }

            byte[] securityToken = await userManager.CreateSecurityTokenAsync(user);
            string modifier = await GetUserModifierAsync(purpose, userManager, user);

            return securityToken != null && RFC6238AuthenticationService.ValidateCode(securityToken, code, modifier);
        }

        /// <summary>
        /// Returns the a value for the user used as entropy in the generated token.
        /// </summary>
        /// <param name="purpose">The purpose of the two-factor authentication token.</param>
        /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve the <paramref name="user"/> from.</param>
        /// <param name="user">The <typeparamref name="TUser"/> to get user id from.</param>
        /// <returns>A string suitable for use as entropy in token generation.</returns>
        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> userManager, TUser user)
        {
            string userId = await userManager.GetUserIdAsync(user);

            return $"Token:{purpose}:{userId}";
        }
    }
}