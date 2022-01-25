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
        private ActionContext ActionContext;
        private readonly IIdentityServerInteractionService InteractionService;
        private readonly IWebHostEnvironment Environment;
        private bool ResourcesDisposed;

        public CorrespondenceService(
            ApplicationDbContext dbContext,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor,
            IIdentityServerInteractionService interactionService,
            IWebHostEnvironment environment
        ) {
            DbContext = dbContext;
            EmailService = emailService;
            ActionContext = actionContextAccessor.ActionContext;
            InteractionService = interactionService;
            Environment = environment;
        }

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

        public async Task<ErrorViewModel> ManageError(string errorId)
        {
            ErrorViewModel viewModel = new ErrorViewModel();

            // retrieve error details from identityserver
            ErrorMessage errorMessage = await InteractionService.GetErrorContextAsync(errorId);

            if (errorMessage != null)
            {
                viewModel.Error = errorMessage;

                if (!Environment.IsDevelopment())
                {
                    // only show in development
                    errorMessage.ErrorDescription = null;
                }
            }

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
