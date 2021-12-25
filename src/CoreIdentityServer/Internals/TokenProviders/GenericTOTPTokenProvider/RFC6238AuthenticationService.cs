/// Ref: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/Rfc6238AuthenticationService.cs

using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CoreIdentityServer.Internals.TokenProviders.GenericTOTPTokenProvider
{
    public static class RFC6238AuthenticationService
    {
        // timestep for TOTP code generation
        private static readonly TimeSpan TimeStep = TimeSpan.FromMinutes(1);
        private static readonly Encoding Encoding = new UTF8Encoding(false, true);

        public static byte[] GenerateRandomKey()
        {
            byte[] bytes = new byte[20];

            RandomNumberGenerator.Fill(bytes);

            return bytes;
        }

        internal static int ComputeTOTP(HashAlgorithm hashAlgorithm, ulong timeStepNumber, string modifier)
        {
            // # of 0's = length of pin
            const int Mod = 1000000;

            // See https://tools.ietf.org/html/rfc4226
            // We can add an optional modifier
            long modifiedTimeStep = IPAddress.HostToNetworkOrder((long) timeStepNumber);
            byte[] timeStepAsBytes = BitConverter.GetBytes(modifiedTimeStep);
            
            byte[] modifiedHashSource = ApplyModifier(timeStepAsBytes, modifier);
            byte[] hash = hashAlgorithm.ComputeHash(modifiedHashSource);

            // generate DT string
            int offset = hash[hash.Length - 1] & 0xf;
            Debug.Assert(offset + 4 < hash.Length);

            int binaryCode = (hash[offset] & 0x7f) << 24
                                | (hash[offset + 1] & 0xff) << 16
                                | (hash[offset + 2] & 0xff) << 8
                                | (hash[offset + 3] & 0xff);

            return binaryCode % Mod;
        }

        // modify an input with given modifier
        private static byte[] ApplyModifier(byte[] input, string modifier)
        {
            if (string.IsNullOrWhiteSpace(modifier))
                return input;
            
            byte[] modifierAsBytes = Encoding.GetBytes(modifier);
            byte[] combinedResult = new byte[checked(input.Length + modifierAsBytes.Length)];

            // combine input and modifier bytes
            Buffer.BlockCopy(input, 0, combinedResult, 0, input.Length);
            Buffer.BlockCopy(modifierAsBytes, 0, combinedResult, input.Length, modifierAsBytes.Length);

            return combinedResult;
        }

        // More info: https://tools.ietf.org/html/rfc6238#section-4
        private static ulong GetCurrentTimeStepNumber()
        {
            TimeSpan delta = DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch;

            return (ulong)(delta.Ticks / TimeStep.Ticks);
        }

        public static int GenerateCode(byte[] securityToken, string modifier = null)
        {
            if (securityToken == null)
            {
                throw new ArgumentNullException(nameof(securityToken));
            }

            // allow a variance of no greater than 3 minutes in either direction
            ulong currentTimeStep = GetCurrentTimeStepNumber();

            using (HMACSHA1 hashAlgorithm = new HMACSHA1(securityToken))
            {
                return ComputeTOTP(hashAlgorithm, currentTimeStep, modifier);
            }
        }

        public static bool ValidateCode(byte[] securityToken, int code, string modifier = null)
        {
            if (securityToken == null)
            {
                throw new ArgumentNullException(nameof(securityToken));
            }

            ulong currentTimeStep = GetCurrentTimeStepNumber();

            using(HMACSHA1 hashAlgorithm = new HMACSHA1(securityToken))
            {
                // allow a variance of no greater than 3 minutes in either direction
                for (int i = -2; i <= 2; i++)
                {
                    int computedTOTP = ComputeTOTP(hashAlgorithm, (ulong)((long)currentTimeStep + i), modifier);

                    if (computedTOTP == code)
                        return true;
                }
            }

            // could not match token for any timeStep
            return false;
        }
    }
}