using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Captcha.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CoreIdentityServer.Internals.Captcha
{
    // Cloudflare Turnstile captcha provider
    public class CloudflareCaptchaProvider : IDisposable
    {
        private string BaseUrl;
        private string SecretKey;
        private readonly HttpClient HttpClient;
        private bool ResourcesDisposed;

        public CloudflareCaptchaProvider (HttpClient httpClient, IConfiguration configuration) {
            HttpClient = httpClient;

            BaseUrl = configuration["cloudflare_captcha:base_url"];
            SecretKey = configuration["cloudflare_captcha:secret_key"];

            if (string.IsNullOrWhiteSpace(BaseUrl) || string.IsNullOrWhiteSpace(SecretKey))
                throw new NullReferenceException("Captcha baseurl/secretkey is missing.");
        }


        /// <summary>
        ///     public async Task<bool> Verify(
        ///         string cfTurnstileResponse,
        ///         IPAddress remoteIpAddress = null
        ///     )
        ///
        ///     Verifies the user's captcha response with the siteverify/ endpoint
        ///      of Cloudflare Turnstile captcha provider.
        ///      
        ///     1. Checks if the user's captcha response or the ip address is null. The
        ///         method returns false if any of them is null.
        ///         
        ///     2. Creates the payload necessary for making a POST request to the
        ///         siteverify/ endpoint using the method CreatePayload().
        ///         
        ///     3. Posts this payload to the siteverify/ endpoint using the method
        ///         PostVerificationRequest() which returns a boolean indicating a
        ///             verification success or failure.
        /// </summary>
        /// <param name="cfTurnstileResponse">User's captcha response</param>
        /// <param name="remoteIpAddress">User's IP address</param>
        /// <returns>Boolean indicating verification success or failure</returns>
        public async Task<bool> Verify(
            string cfTurnstileResponse,
            IPAddress remoteIpAddress = null
        ) {
            if (string.IsNullOrWhiteSpace(cfTurnstileResponse) || remoteIpAddress == null)
            {
                return false;
            }

            StringContent verificationRequestPayload = CreatePayload(cfTurnstileResponse, remoteIpAddress);

            return await PostVerificationRequest(verificationRequestPayload);
        }


        /// <summary>
        ///     private StringContent CreatePayload(
        ///         string cfTurnstileResponse, IPAddress userIPAddress
        ///     )
        ///     
        ///     Creates the payload for the POST request to siteverify/ endpoint.
        /// </summary>
        /// <param name="cfTurnstileResponse">User's captcha response</param>
        /// <param name="userIPAddress">User's IP address</param>
        /// <returns>Encoded payload</returns>
        private StringContent CreatePayload(string cfTurnstileResponse, IPAddress userIPAddress)
        {
            VerifyEndpointPayloadModel requestPayload = new VerifyEndpointPayloadModel {
                Secret = SecretKey,
                Response = cfTurnstileResponse,
                RemoteIP = userIPAddress.ToString()
            };

            string jsonSerializedPayload = JsonConvert.SerializeObject(requestPayload);

            return new StringContent(jsonSerializedPayload, Encoding.UTF8, "application/json");
        }


        /// <summary>
        ///     private async Task<bool> PostVerificationRequest(StringContent payload)
        ///     
        ///     Makes a POST request to the siteverify/ endpoint with the created payload.
        ///     
        ///     1. Using a try-catch block, makes a POST request to the siteverify/
        ///         endpoint in the try block.
        ///     
        ///     2. If the response came with a success status code, the response body
        ///         is parsed as JSON and mapped to the VerifyEndpointResponseModel
        ///             class instance. The response status code and endpoint is also
        ///                 printed to the console.
        ///             
        ///        The method then returns the responseBody's Success property which
        ///         is a boolean indicating captcha verification success or failure.
        ///         
        ///     3. If the response came with a non-success status code, the method
        ///         returns false indicating a verification failure and prints the
        ///             response status code and endpoint to the console.
        ///             
        ///     4. In case an exception was encountered in the try block, then the
        ///         method throws this exception if it is not one of the expected
        ///             exceptions. Otherwise, the method prints the exception details
        ///              to the console and returns false to indicate captcha
        ///                 verification failure.
        /// </summary>
        /// <param name="payload">Payload to post to the siteverify/ endpoint</param>
        /// <returns>Boolean indicating captcha verifciation success or failure</returns>
        private async Task<bool> PostVerificationRequest(StringContent payload)
        {
            string verifyEndpoint = BaseUrl + "/siteverify";

            try
            {
                HttpResponseMessage httpResponse = await HttpClient.PostAsync(
                    verifyEndpoint, payload
                );
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine(
                        "Response from Cloudflare siteverify endpoint: {0} status code: {1}",
                        verifyEndpoint,
                        (int)httpResponse.StatusCode
                    );

                    VerifyEndpointResponseModel responseBody = await httpResponse.Content.ReadFromJsonAsync<VerifyEndpointResponseModel>();

                    return responseBody.Success;
                }
                else
                {
                    Console.WriteLine(
                        "Response from Cloudflare siteverify endpoint: {0} status code: {1}",
                        verifyEndpoint,
                        (int)httpResponse.StatusCode
                    );
                
                    return false;
                }
            }
            catch (Exception exception)
            {
                if (
                    exception is InvalidOperationException ||
                    exception is HttpRequestException ||
                    exception is TaskCanceledException ||
                    exception is UriFormatException
                ) {
                    Console.WriteLine(
                        "Exception invoking Cloudflare siteverify endpoint for url: {0}",
                        verifyEndpoint
                    );

                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.InnerException);

                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public void Dispose()
        {
            // clean up
            if (ResourcesDisposed)
                return;

            ResourcesDisposed = true;
        }
    }
}
