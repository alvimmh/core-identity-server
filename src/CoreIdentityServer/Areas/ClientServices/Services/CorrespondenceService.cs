using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Areas.ClientServices.Models.Correspondence;
using CoreIdentityServer.Internals.Services.Email;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CoreIdentityServer.Areas.ClientServices.Services
{
    public class CorrespondenceService : BaseService, IDisposable
    {
        private readonly ApplicationDbContext DbContext;
        private EmailService EmailService;
        private readonly IIdentityServerInteractionService InteractionService;
        private readonly IWebHostEnvironment Environment;
        private bool ResourcesDisposed;

        public CorrespondenceService(
            ApplicationDbContext dbContext,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor,
            IIdentityServerInteractionService interactionService,
            IWebHostEnvironment environment
        ) : base(actionContextAccessor)
        {
            DbContext = dbContext;
            EmailService = emailService;
            InteractionService = interactionService;
            Environment = environment;
        }


        /// <summary>
        ///     public async Task<bool> ResendEmail(ResendEmailInputModel inputModel)
        ///     
        ///     Manages the ResendEmail POST action.
        ///     
        ///     1. Checks if the ModelState is valid. If not, an error message is set in
        ///         the input model for the user. Then the method returns false.
        ///         
        ///     2. Fetches the email record object using the method DbContext.EmailRecords.FindAsync().
        ///         If the record is not found, an error message is set in the input model for the
        ///             user. And the method returns false.
        ///             
        ///     3. If the email record was found, the input model is checked with the record to see
        ///         if a resend is possible. If not, the method returns false.
        ///         
        ///     4. If it is possible to resend the email, the email is resent using the method
        ///         EmailService.ResendEmail(). Finally, the method returns true.
        /// </summary>
        /// <param name="inputModel">The input model containing information about the email record</param>
        /// <returns>Boolean indicating if the email was resent or not</returns>
        public async Task<bool> ResendEmail(ResendEmailInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
            {
                inputModel.SetErrorMessage("Could not resend email");

                return false;
            }

            EmailRecord emailRecord = await DbContext.EmailRecords.FindAsync(inputModel.ResendEmailRecordId);

            if (emailRecord == null)
            {
                inputModel.SetErrorMessage("Could not resend email");

                return false;
            }

            bool canResendEmail = emailRecord.CanResendEmail(inputModel);

            if (!canResendEmail)
            {
                return false;
            }

            EmailService.ResendEmail(emailRecord);

            return true;
        }


        /// <summary>
        ///     public async Task<ErrorViewModel> ManageDuendeIdentityServerError(string errorType)
        ///     
        ///     Manages errors encountered from the Duende Identity Server internally.
        /// </summary>
        /// <param name="errorType">The type of the error</param>
        /// <returns>A view model object containing the error</returns>
        public async Task<ErrorViewModel> ManageDuendeIdentityServerError(string errorType)
        {
            string errorId = errorType;

            ErrorViewModel viewModel = new ErrorViewModel();

            // retrieve error details from identityserver
            ErrorMessage errorMessage = await InteractionService.GetErrorContextAsync(errorId);

            if (errorMessage != null)
            {
                if (!Environment.IsDevelopment())
                {
                    // only show in development
                    errorMessage.ErrorDescription = null;
                }

                viewModel.Error = errorMessage;
            }

            return viewModel;
        }


        /// <summary>
        ///     public ErrorViewModel ManageError(string errorType)
        ///     
        ///     Manages errors encountered in the application, excluding Duende Identity Server
        ///         internal errors.
        /// </summary>
        /// <param name="errorType">The type of the error</param>
        /// <returns>A view model containing the error</returns>
        public ErrorViewModel ManageError(string errorType)
        {
            string statusCode = errorType;

            ErrorViewModel viewModel = new ErrorViewModel(statusCode);

            return viewModel;
        }


        // clean up to be done by DI
        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            DbContext.Dispose();
            ResourcesDisposed = true;
        }
    }
}
