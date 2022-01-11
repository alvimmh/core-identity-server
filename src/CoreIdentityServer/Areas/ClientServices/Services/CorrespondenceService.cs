using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Areas.ClientServices.Models.Correspondence;
using CoreIdentityServer.Internals.Services.Email;

namespace CoreIdentityServer.Areas.ClientServices.Services
{
    public class CorrespondenceService : BaseService, IDisposable
    {
        private readonly ApplicationDbContext DbContext;
        private EmailService EmailService;
        private ActionContext ActionContext;
        private bool ResourcesDisposed;

        public CorrespondenceService(
            ApplicationDbContext dbContext,
            EmailService emailService,
            IActionContextAccessor actionContextAccessor
        ) {
            DbContext = dbContext;
            EmailService = emailService;
            ActionContext = actionContextAccessor.ActionContext;
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
