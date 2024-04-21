using System;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace CoreIdentityServer.Internals.Services.Email
{
    public class EmailService : IDisposable
    {
        private ApplicationDbContext DbContext;
        private SMTPService SMTPService;

        public EmailService(ApplicationDbContext dbContext, SMTPService smtpService)
        {
            DbContext = dbContext;
            SMTPService = smtpService;
        }


        /// <summary>
        ///     private async Task<string> SendEmail(
        ///         string smtpFrom, string smtpTo, string subject, string body, bool recordEmail = false
        ///     )
        ///     
        ///     Sends emails for the application.
        ///     
        ///     1. Checks if the email to be sent needs to be recorded for resending purpose. If true,
        ///         a new email record is created and saved in the database. The id of the record is
        ///             used as an event for logging purposes.
        ///         
        ///     2. Then the email is sent using the SMTPService.SendAsync() method.
        /// </summary>
        /// <param name="smtpFrom">From email address</param>
        /// <param name="smtpTo">To email address</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="body">Body of the email</param>
        /// <param name="recordEmail">Boolean indicating if the email needs to be recorded</param>
        /// <returns>the id of the email record or null</returns>
        private async Task<string> SendEmail(string smtpFrom, string smtpTo, string subject, string body, bool recordEmail = false)
        {
            EmailRecord emailRecord = null;

            if (recordEmail)
            {
                emailRecord = new EmailRecord();
                emailRecord.SetRecordDetails(smtpFrom, smtpTo, subject, body, DateTime.UtcNow);

                await DbContext.EmailRecords.AddAsync(emailRecord);
                await DbContext.SaveChangesAsync();
            }

            string sendEmailEventId = recordEmail ? emailRecord.Id : null;

            SMTPService.SendAsync(smtpFrom, smtpTo, subject, body, sendEmailEventId);
        
            return sendEmailEventId;
        }


        /// <summary>
        ///     public void ResendEmail(EmailRecord emailRecord)
        ///     
        ///     Resends an email using the SMTPService.SendAsync() method.
        /// </summary>
        /// <param name="emailRecord">Email record containing data for the resend email</param>
        public void ResendEmail(EmailRecord emailRecord)
        {
            SMTPService.SendAsync(emailRecord.SentFrom, emailRecord.SentTo, emailRecord.Subject, emailRecord.Body, emailRecord.Id);
        }

        
        /// <summary>
        ///     public async Task DeleteEmailRecord(string resendEmailRecordId, ApplicationUser user)
        ///     
        ///     Deletes an email record.
        ///     
        ///     1. Fetches the email record using the id. If record is not found, an error message
        ///         is printed to the console mentioning the record id.
        ///         
        ///     2. Checks if the record's 'to' address matches the user's email address. If it doesn't
        ///         match, the record belongs to someone else, so it can't be deleted.
        ///     
        ///     3. Using a try-catch block, the email record is deleted, or if fails to delete, any
        ///         exception is caught, and the error is logged or thrown depending on the type.
        /// </summary>
        /// <param name="resendEmailRecordId">Id of the email record</param>
        /// <param name="user">The ApplicationUser object</param>
        /// <returns>void</returns>
        public async Task DeleteEmailRecord(string resendEmailRecordId, ApplicationUser user)
        {
            EmailRecord emailRecord = await DbContext.EmailRecords.FindAsync(resendEmailRecordId);

            if (emailRecord == null)
            {
                Console.WriteLine($"Could not find email record with id {resendEmailRecordId} to delete.");
            }
            else
            {
                if (emailRecord.SentTo == user.Email)
                {
                    try
                    {
                        DbContext.EmailRecords.Remove(emailRecord);

                        await DbContext.SaveChangesAsync();
                    }
                    catch (Exception exception)
                    {
                        if (exception is DbUpdateException || exception is DbUpdateConcurrencyException)
                        {
                            Console.WriteLine("Could not delete email record: {0}", exception.Message);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Cannot delete email record. User email doesn't match with recipient email address of email record.");
                }
            }
        }

        // send a reminder to user to reset authenticator
        public async Task SendResetTOTPAccessReminderEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "Please Reset TOTP Access";
            string emailBody = $"Dear {userName}, you previously tried to reset your TOTP authenticator but did not reset it completely. To keep your account secure, we have blocked your latest Sign In attempt. Please finish resetting your authenticator to Sign In.";

            await SendEmail(emailFrom, emailTo, emailSubject, emailBody);
        }

        // send a verification code to verify user's identity before resetting TOTP access
        public async Task<string> SendResetTOTPAccessVerificationEmail(string emailFrom, string emailTo, string userName, string verificationCode)
        {
            string emailSubject = "Please Confirm Your Identity";
            string emailBody = $"Greetings {userName}, please confirm you identity by submitting this verification code: {verificationCode}";

            string emailRecordId = await SendEmail(emailFrom, emailTo, emailSubject, emailBody, true);

            return emailRecordId;
        }

        // send a verification code to verify user's email address
        public async Task<string> SendEmailConfirmationEmail(string emailFrom, string emailTo, string userName, string verificationCode)
        {
            string emailSubject = "Please Confirm Your Email";
            string emailBody = $"Greetings {userName}, please confirm your email by submitting this verification code: {verificationCode}";

            string emailRecordId = await SendEmail(emailFrom, emailTo, emailSubject, emailBody, true);

            return emailRecordId;
        }

        // user email confirmed, notify user
        public async Task SendEmailConfirmedEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "Email Confirmed";
            string emailBody = $"Congratulations {userName}, your email is now verified.";

            await SendEmail(emailFrom, emailTo, emailSubject, emailBody);
        }

        // send a verification code to user's email
        public async Task<string> SendNewSessionVerificationEmail(string emailFrom, string emailTo, string userName, string verificationCode)
        {
            string emailSubject = "Please Confirm New Session";
            string emailBody = $"Greetings, please confirm new sign in by submitting this verification code: {verificationCode}";

            // user account successfully created, initiate email confirmation
            string emailRecordId = await SendEmail(emailFrom, emailTo, emailSubject, emailBody, true);

            return emailRecordId;
        }

        // notify the user about account lockout
        public async Task SendAccountLockedOutEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "Account Locked Out";
            string emailBody = $"Dear {userName}, due to 3 unsuccessful attempts to sign in to your account, we have locked it out. You can try again in 30 minutes or click this link to reset your TOTP access.";

            await SendEmail(emailFrom, emailTo, emailSubject, emailBody);
        }

        // notify user about new session
        public async Task SendNewActiveSessionNotificationEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "New Active Session Started";
            string emailBody = $"Dear {userName}, this is to notify you of a new active session.";

            await SendEmail(emailFrom, emailTo, emailSubject, emailBody);
        }

        // notify user to complete account registration
        public async Task SendAccountNotRegisteredEmail(string emailFrom, string emailTo, string userName)
        {
            string emailSubject = "SignIn Attempt Detected";
            string emailBody = $"Dear {userName}, we have detected a sign in attempt for your account. To log in, you need to finish registration.";

            await SendEmail(emailFrom, emailTo, emailSubject, emailBody);
        }

        // send an email about latest two factor authentication changes
        public async Task SendNewTwoFactorAuthenticationStatusEmail(string emailFrom, string emailTo, string userName, bool twoFactorEnabled)
        {
            string twoFactorAuthenticationStatus = twoFactorEnabled ? "Enabled" : "Disabled";

            string emailSubject = $"Two Factor Authentication {twoFactorAuthenticationStatus}";
            string emailBody = $"Dear {userName}, two factor authentication via email has been {twoFactorAuthenticationStatus.ToLower()} for your account. Thank you.";

            await SendEmail(emailFrom, emailTo, emailSubject, emailBody);
        }

        // send a TOTP Access recovery code to CIS product owner
        public void SendProductOwnerTOTPAccessRecoveryCodeEmail(string emailFrom, string emailTo, string userName, string recoveryCode)
        {
            string emailSubject = "Product Owner Credentials";
            string emailBody = $"Greetings {userName}, please sign in to your account by resetting TOTP Access using this recovery code: {recoveryCode}. You can update your profile settings after signing in by visiting the '/vault/profile/index' page.";

            SMTPService.Send(emailFrom, emailTo, emailSubject, emailBody);
        }

        public void Dispose()
        {
            if (DbContext != null)
            {
                DbContext.Dispose();
                DbContext = null;
            }
        }
    }
}
