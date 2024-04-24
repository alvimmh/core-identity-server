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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Models.InputModels;
using CoreIdentityServer.Internals.Constants.Authorization;
using System.Linq;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using CoreIdentityServer.Internals.Constants.Events;
using CoreIdentityServer.Internals.Models.ViewModels;
using Mapster;
using System.Net.Http;
using CoreIdentityServer.Internals.Constants.Administration;
using CoreIdentityServer.Internals.Constants.Errors;

namespace CoreIdentityServer.Internals.Services.BackChannelCommunications
{
    public class BackChannelNotificationService : BaseService, IDisposable
    {
        private readonly IBackChannelLogoutService DefaultBackChannelLogoutService;
        private OIDCTokenService OIDCTokenService;
        private readonly HttpClient HttpClient;
        private bool ResourcesDisposed;

        public BackChannelNotificationService(
            IBackChannelLogoutService defaultBackChannelLogoutService,
            OIDCTokenService oidcTokenService,
            HttpClient httpClient
        ) {
            DefaultBackChannelLogoutService = defaultBackChannelLogoutService;
            OIDCTokenService = oidcTokenService;
            HttpClient = httpClient;
        }


        /// <summary>
        ///     public async Task SendBackChannelLogoutNotificationsForUserAsync(
        ///         ApplicationUser user
        ///     )
        ///     
        ///     Sends a back-channel logout notification to all identity server clients
        ///         to logout a user.
        ///     
        ///     1. Lists all the clients and creates a logout context with the user's id.
        ///     
        ///     2. Sends this context to all clients using the method
        ///         DefaultBackChannelLogoutService.SendLogoutNotificationsAsync().
        ///         
        ///     3. This will trigger back-channel logout notifications to all clients
        ///         if the user is persisted in the client, the client will sign out the user
        ///             otherwise the client will return a failed HTTP response.
        ///     
        ///        Note, different clients may implement different types of user authentication
        ///         system, this method only sends the back-channel notification for these clients.
        ///             It is the client's responsibility to signout the user from their system.
        /// </summary>
        /// <param name="user">The user for whom the notification is being sent</param>
        /// <returns>void</returns>
        public async Task SendBackChannelLogoutNotificationsForUserAsync(ApplicationUser user)
        {
            List<string> allClients = Config.Clients.Select(client => client.ClientId).ToList();

            // note, SessionId is required, so not setting it to null
            LogoutNotificationContext blockedUserLogoutNotificationContext = new LogoutNotificationContext()
            {
                SubjectId = user.Id,
                ClientIds = allClients,
                SessionId = string.Empty
            };

            await DefaultBackChannelLogoutService.SendLogoutNotificationsAsync(blockedUserLogoutNotificationContext);
        }


        /// <summary>
        ///     public async Task<IdentityResult> SendBackChannelDeleteNotificationsForUserAsync(
        ///         ApplicationUser user
        ///     )
        ///     
        ///     Sends a back-channel delete notification to all identity server
        ///         clients to delete a user.
        ///         
        ///     1. Lists all clients and creates an empty list of all outbound notifications.
        ///     
        ///     2. For each client, a CreateTokenInputModel object is created providing it
        ///         with the user id as subject id and the client id.
        ///         
        ///        Then using the method OIDCTokenService.CreateTokenAsync() a JWT notification
        ///         token is created with data from the input model object.
        ///         
        ///        The current client's back-channel delete notification is declared in a
        ///         variable. Now a notification request object is constructed for the current
        ///             client setting all necessary details for the notification.
        ///             
        ///        Finally, the request is added to the notification requests list.
        ///        
        ///     3. Now, for each request in the notification requests array, the method
        ///         SendBackChannelNotificationAsync() is called and the results are stored in
        ///             an array.
        ///             
        ///     4. If any of the results failed, the array is checked if at least one of
        ///         the result passed to determine if the results succeeded partially. If
        ///             it succeeded partially, an error message is constructed and is added
        ///                 to the IdentityResult object with a description of
        ///                     InternalCustomErrors.UserPartiallyDeleted. The method then
        ///                         returns the IdentityResult object by marking it failed.
        ///                     
        ///     5. If none of the results succeeded in the array, the method returns the
        ///         IdentityResult object by marking it failed. It does not add any error
        ///             message in this case.
        ///             
        ///     6. If all results mentioned in step 3 succeeded, the method returns an
        ///         IdentityResult object by marking it successful.
        /// </summary>
        /// <param name="user">The user for whom the notifications were sent</param>
        /// <returns>IdentityResult object of success or failure</returns>
        public async Task<IdentityResult> SendBackChannelDeleteNotificationsForUserAsync(ApplicationUser user)
        {
            List<ClientNotificationViewModel> allClients = Config.Clients
                                                                    .Select(client => client.Adapt<ClientNotificationViewModel>())
                                                                    .ToList();

            List<BackChannelNotificationRequest> notificationRequests = new List<BackChannelNotificationRequest>();

            foreach (ClientNotificationViewModel client in allClients)
            {
                CreateTokenInputModel inputModel = new CreateTokenInputModel()
                {
                    SubjectId = user.Id,
                    ClientId = client.ClientId
                };

                string notificationJWTToken = await OIDCTokenService.CreateTokenAsync(inputModel, OIDCTokenEvents.BackChannelDelete);

                string notificationEndpoint = $"{client.ClientUri}/administration/authentication/delete_oidc";

                BackChannelNotificationRequest notificationRequest = new BackChannelNotificationRequest()
                {
                    NotificationType = BackChannelNotificationTypes.Delete,
                    JWTToken = notificationJWTToken,
                    ClientId = client.ClientId,
                    ClientNotificationUri = notificationEndpoint
                };

                notificationRequests.Add(notificationRequest);
            }

            Task<bool>[] sendNotificationTasks = notificationRequests.Select(SendBackChannelNotificationAsync).ToArray();
        
            bool[] sendNotificationTaskResults = await Task.WhenAll<bool>(sendNotificationTasks);

            bool tasksSucceeded = !sendNotificationTaskResults.Any(element => element == false);

            if (!tasksSucceeded)
            {
                bool tasksSucceededPartially = sendNotificationTaskResults.Any(element => element == true);

                // user was deleted from some clients but not all
                if (tasksSucceededPartially)
                {
                    IdentityError error = new IdentityError()
                    {
                        Description = InternalCustomErrors.UserPartiallyDeleted
                    };

                    return IdentityResult.Failed(error);
                }

                return IdentityResult.Failed();
            }

            return IdentityResult.Success;
        }


        /// <summary>
        ///     private async Task<bool> SendBackChannelNotificationAsync(
        ///         BackChannelNotificationRequest request
        ///     )
        ///     
        ///     Creates the payload data for a back-channel notification with
        ///         information in params and posts the notification to a client.
        /// </summary>
        /// <param name="request">
        ///     The BackChannelNotificationRequest object containing
        ///         the NotificationType, JWTToken, ClientId and the ClientNotificationUri.
        /// </param>
        /// <returns>
        ///     Boolean indicating a successful or failed response from the client
        /// </returns>
        private async Task<bool> SendBackChannelNotificationAsync(BackChannelNotificationRequest request)
        {
            Dictionary<string, string> data = CreateFormPostPayloadAsync(request);

            return await PostBackChannelNotificationJwt(request, data);
        }


        /// <summary>
        ///     private Dictionary<string, string> CreateFormPostPayloadAsync(
        ///         BackChannelNotificationRequest request
        ///     )
        ///     
        ///     Creates the payload for a back-channel notification request.
        /// </summary>
        /// <param name="request">
        ///     BackChannelNotificationRequest object containing data to construct the payload
        /// </param>
        /// <returns>
        ///     A dictionary of strings as the payload for the back-channel notification
        /// </returns>
        private Dictionary<string, string> CreateFormPostPayloadAsync(BackChannelNotificationRequest request)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { CustomTokenOptions.BackChannelDeleteTokenPostBodyKey, request.JWTToken }
            };

            return data;
        }


        /// <summary>
        ///     private Task<bool> PostBackChannelNotificationJwt(
        ///         BackChannelNotificationRequest request,
        ///         Dictionary<string, string> data
        ///     )
        ///     
        ///     Posts the back-channel notification by calling the method
        ///         PostBackChannelNotificationAsync().
        /// </summary>
        /// <param name="request">
        ///     BackChannelNotificationRequest object containing the data
        ///         about the posting uri and notification type
        /// </param>
        /// <param name="data">Payload for the POST request</param>
        /// <returns>Boolean indicating a success or failed response</returns>
        private Task<bool> PostBackChannelNotificationJwt(
            BackChannelNotificationRequest request,
            Dictionary<string, string> data
        ) {
            return PostBackChannelNotificationAsync(request.ClientNotificationUri, data, request.NotificationType);
        }


        /// <summary>
        ///     private async Task<bool> PostBackChannelNotificationAsync(
        ///         string url, Dictionary<string, string> payload, string notificationType
        ///     )
        ///     
        ///     Posts the back-channel notification to the client.
        ///     
        ///     1. Using a try-catch block, the back-channel notification is posted
        ///         to the client.
        ///         
        ///     2. If the client sends a success status code, information about the response
        ///         is logged to the console. Then the method returns true.
        ///             
        ///     3. If the client sends a failed status code, information about the response
        ///         is logged to the console. Then the method returns false.
        ///         
        ///     4. In case there was an exception in the try block, the exception is printed
        ///         to the console and the method returns false.
        /// </summary>
        /// <param name="url">Url to post the back-channel notification</param>
        /// <param name="payload">Payload for the notification POST request</param>
        /// <param name="notificationType">Type of the notification, logout or delete</param>
        /// <returns>Boolean indicating successful or failed response</returns>
        private async Task<bool> PostBackChannelNotificationAsync(
            string url,
            Dictionary<string, string> payload,
            string notificationType
        ) {
            try
            {
                HttpResponseMessage response = await HttpClient.PostAsync(url, new FormUrlEncodedContent(payload));
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Response from back-channel {0} notification endpoint: {1} status code: {2}", notificationType, url, (int)response.StatusCode);

                    return true;
                }
                else
                {
                    Console.WriteLine("Response from {0} notification endpoint: {1} status code: {2}", notificationType, url, (int)response.StatusCode);
                
                    return false;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception invoking back-channel {0} for url: {1}", notificationType, url);
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.InnerException);

                return false;
            }
        }


        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            ResourcesDisposed = true;
        }
    }
}
