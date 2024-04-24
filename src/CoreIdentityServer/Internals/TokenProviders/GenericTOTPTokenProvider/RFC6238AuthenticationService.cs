// Copyright (c) .NET Foundation and Contributors
// All rights reserved.
//
// For more information visit:
// https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt
//
// or, alternatively you can view the license in the project's ~/Licenses/ASP.NETCoreLicense.txt file

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
        // timestepSpan for TOTP code generation
        private static readonly TimeSpan TimeStepSpan = TimeSpan.FromMinutes(1);
        private static readonly Encoding Encoding = new UTF8Encoding(false, true);


        /// <summary>
        ///     public static byte[] GenerateRandomKey()
        ///     
        ///     Generates a random security key.
        /// 
        ///     1. This function creates a new byte array.
        ///     
        ///     2. Fills the byte array with random bytes using the RandomNumberGenerator function.
        /// 
        ///     For testing purpose.
        /// </summary>
        /// <returns>The random bytes[] array to serve as a security token.</returns>
        public static byte[] GenerateRandomKey()
        {
            byte[] bytes = new byte[20];

            RandomNumberGenerator.Fill(bytes);

            return bytes;
        }


        /// <summary>
        ///     private static byte[] ApplyModifier(byte[] input, string modifier)
        ///     
        ///     Applies a modifier to an input array of bytes.
        /// 
        ///     1. Encodes the modifier and stores it in the modifierAsBytes array.
        ///     
        ///     2. Copies a specified number of bytes (input.Length) from a source array (the input param)
        ///         starting at a particular offset (0) to a destination array (combinedResult variable)
        ///             starting at a particular offset (0).
        ///     
        ///     3. Copies a specified number of bytes (modifierAsBytes.Length) from a source
        ///         array (modifiedAsBytes variable) starting at a particular offset (0) to a destination
        ///             array (combinedResult variable) starting at a particular offset (input.Length).
        /// </summary>
        /// <param name="input">
        ///     The input byte array on which to apply the modifier
        /// </param>
        /// <param name="modifier">
        ///     String to use as a modifier
        /// </param>
        /// <returns>
        ///     Modified input bytes[] array (the combinedResult variable)
        /// </returns>
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


        /// <summary>
        ///     private static ulong GetCurrentTimeStepNumber()
        ///     
        ///     Gets the current time step number from the Unix epoch to the current UTC time.
        /// 
        ///     1. Calculates the timespan from Unix epoch till now in UTC.
        ///     
        ///     2. Calculates the total time steps: dividing total ticks in the timespan by total
        ///         ticks in TimeStepSpan. The TimeStepSpan is a property of this class representing
        ///             the TimeSpan used as a relative unit.
        ///        
        ///        More info: https://tools.ietf.org/html/rfc6238#section-4
        /// </summary>
        /// <returns>
        ///     The timesSteps variable in ulong form
        /// </returns>
        private static ulong GetCurrentTimeStepNumber()
        {
            TimeSpan delta = DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch;
            
            long timeSteps = delta.Ticks / TimeStepSpan.Ticks;

            return (ulong)timeSteps;
        }


        /// <summary>
        ///     internal static int ComputeTOTP(
        ///         HashAlgorithm hashAlgorithm, ulong timeStepNumber, string modifier
        ///     )
        ///     
        ///     Computes a TOTP code following the RFC6238 memo.
        ///     
        ///     1. Sets the length of the TOTP code in the mod variable, represented
        ///         by the number of zeroes in it.
        ///     
        ///     2. Modifies the timeStepNumber param by converting it from Host Byte Order to Network Byte Order.
        ///         Then converts the modifiedTimeStep as bytes inside the timeStepAsBytes variable.
        ///     
        ///     3. Modifies the timeStepAsBytes with the modifier param using the ApplyModifier() method
        ///         and takes it as the hash source, stored in the modifiedHashSource variable.
        ///     
        ///     4. Then computes the hash from the hash source (modifiedHashSource variable) using the
        ///         ComputeHash function of the hashAlgorithm (hashAlgorithm param).
        ///     
        ///     5. Creates an offset (the offset variable) by calculating the bitwise AND of the last byte of the
        ///         hash array and 0xf (binary 1111). A debug assertion is included so the offset does not go out of
        ///             bounds of the hash array, for development environments.
        ///     
        ///     6. The binary TOTP code in integer form (the binaryCode variable) is calculated by following these steps:
        ///         
        ///         i. Calculates the bitwise AND of the last byte of the hash array and 0x7f (binary 0111 1111). Then
        ///             left-shifts the result by 24 bits. Using 0x7f ensures the most significant bit is positive.
        ///         
        ///         ii. Calculates the bitwise AND of the 2nd last byte of the hash array and 0xff (binary 1111 1111).
        ///             Then left-shifts the result by 16 bits.
        ///         
        ///         iii. Calculates the bitwise AND of the 3rd last byte of the hash array and 0xff (binary 1111 1111).
        ///             Then left-shifts the result by 8 bits.
        ///         
        ///         iv. Calculates the bitwise AND of the 4th last byte of the hash array and 0xff (binary 1111 1111).
        ///         
        ///         v. Calculates the bitwise OR of the result of step i, ii, iii and iv.
        ///         
        ///        In the above procedure, left-shifting the results of bitwise AND operations by 24, 16 and 8 bits are
        ///         necessary to form the final 32 bit integer number.
        ///         
        ///     7. Gets the TOTP code (TOTPCode variable) by calculating the modulus of the
        ///         binary code (binaryCode variable) and the mod (mod variable). This ensures the TOTP code
        ///             has a length equal to the number of zeroes in the mod.
        /// </summary>
        /// <param name="hashAlgorithm">Algorithm used to generate the hash for the TOTP code</param>
        /// <param name="timeStepNumber">Time step number, using which the TOTP code will be generated</param>
        /// <param name="modifier">The string used to modify the hash source before computing the hash</param>
        /// <returns>The TOTP code</returns>
        internal static int ComputeTOTP(HashAlgorithm hashAlgorithm, ulong timeStepNumber, string modifier)
        {
            // # of 0's = length of pin
            const int mod = 1000000;

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

            int TOTPCode = binaryCode % mod;

            return TOTPCode;
        }


        /// <summary>
        ///     public static int GenerateCode(byte[] securityToken, string modifier = null)
        ///     
        ///     Generates a TOTP code.
        /// </summary>
        /// <param name="securityToken">
        ///     Security token used to initialize the HMACSHA1 class which computes the
        ///         hash for the TOTP code
        /// </param>
        /// <param name="modifier">
        ///     The string used to modify the hash source
        /// </param>
        /// <returns>The generated TOTP code</returns>
        /// <exception cref="ArgumentNullException">
        ///     Exception thrown when the securityToken param is missing
        /// </exception>
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


        /// <summary>
        ///     public static bool ValidateCode(byte[] securityToken, int code, string modifier = null)
        ///     
        ///     Validates a TOTP code.
        ///     
        ///     1. Checks if the securityToken param is missing. If it is missing, throws an exception.
        ///     
        ///     2. Gets the current time step number using the method GetCurrentTimeStepNumber().
        ///     
        ///     3. Initializes a new HMACSHA1 class with the securityToken param, to generate the
        ///         hash for the TOTP code.
        ///         
        ///     4. With a variance of no greater than 3 mins in the past or future, TOTP codes are
        ///         generated using the ComputeTOTP() method. If any of the generated code matches
        ///             the code to validate, the method returns true. Otherwise, the method returns
        ///                 false.
        /// </summary>
        /// <param name="securityToken">
        ///     Security token used to create the HMACSHA1 class, which computes the hash
        ///         for the TOTP code
        /// </param>
        /// <param name="code">
        ///     TOTP code to validate
        /// </param>
        /// <param name="modifier">
        ///     The string used to modify the hash source
        /// </param>
        /// <returns>
        ///     Boolean indicating the validation result of the TOTP code
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Exception thrown when the securityToken param is missing
        /// </exception>
        public static bool ValidateCode(byte[] securityToken, int code, string modifier = null)
        {
            if (securityToken == null)
            {
                throw new ArgumentNullException(nameof(securityToken));
            }

            ulong currentTimeStep = GetCurrentTimeStepNumber();

            using (HMACSHA1 hashAlgorithm = new HMACSHA1(securityToken))
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