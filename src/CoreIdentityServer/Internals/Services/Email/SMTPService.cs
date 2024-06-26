using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CoreIdentityServer.Internals.Services.Email
{
    public class SMTPService : IDisposable
    {
        private string SmtpHost;
        private int SmtpPort;
        private string SmtpUsername;
        private string SmtpPassword;
        private SmtpClient SmtpClient;
        private DbContextOptionsBuilder<ApplicationDbContext> OptionsBuilder;


        /// <summary>
        ///     public SMTPService(IConfiguration configuration)
        ///     
        ///     Constructs and configures the SMTPService and
        ///         adds event handler for send completed event.
        /// </summary>
        /// <param name="configuration">Configuration for this application</param>
        /// <exception cref="NullReferenceException">
        ///     Throws this exception when SMTP credentials are missing,
        ///         or, when the database connection and credentials are missing.
        /// </exception>
        public SMTPService(IConfiguration configuration) {

            // configure SMTP client
            SmtpHost = configuration["smtp:host"];
            SmtpUsername = configuration["smtp:username"];
            SmtpPassword = configuration["smtp:password"];
            
            bool isSmtpPortValid = int.TryParse(configuration["smtp:port"], out SmtpPort);

            if (
                string.IsNullOrWhiteSpace(SmtpHost) ||
                string.IsNullOrWhiteSpace(SmtpUsername) ||
                string.IsNullOrWhiteSpace(SmtpPassword) ||
                !isSmtpPortValid
            ) {
                throw new NullReferenceException("SMTP credentials are missing");
            }

            SmtpClient = new SmtpClient(SmtpHost, SmtpPort) {
                Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl = true
            };

            // add send completed event handler
            SmtpClient.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);

            string mainDbHost = configuration["cis_main_database:host"];
            string mainDbName = configuration["cis_main_database:name"];
            int mainDbPort;

            bool isMainDbPortValid = int.TryParse(configuration["cis_main_database:port"], out mainDbPort);

            if (string.IsNullOrWhiteSpace(mainDbHost) ||
                string.IsNullOrWhiteSpace(mainDbName) ||
                !isMainDbPortValid
            ) {
                throw new NullReferenceException("Main database connection string is missing.");
            }

            string mainDbUserName = configuration["cis_main_database:username"];
            string mainDbPassword = configuration["cis_main_database:password"];

            if (string.IsNullOrWhiteSpace(mainDbUserName) || string.IsNullOrWhiteSpace(mainDbPassword))
                throw new NullReferenceException("Main database credentials are missing.");

            NpgsqlConnectionStringBuilder mainDbConnectionBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = mainDbHost,
                Port = mainDbPort,
                Database = mainDbName,
                Username = mainDbUserName,
                Password = mainDbPassword
            };

            OptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            OptionsBuilder.UseNpgsql(mainDbConnectionBuilder.ConnectionString);
        }


        /// <summary>
        ///     private async void SendCompletedCallback(object sender, AsyncCompletedEventArgs eventArgs)
        ///     
        ///     Callback method when an email is send operation completes.
        ///     
        ///     1. Checks the event arguments if the email send operation was cancelled. If so, logs
        ///         the event and marks the associated email record as cancelled if the email was recorded.
        ///         
        ///     2. In case the email send operation was not cancelled but encountered an error, logs
        ///         the event and marks the associated email record as cancelled if the email was recorded.
        ///         
        ///     3. In case the email send operation was not cancelled neither encountered an error, it
        ///         means the email send operation was successful. Then the event is logged, and the
        ///             associated email record is marked as sent if the email was recorded.
        /// </summary>
        /// <param name="sender">Source of the event</param>
        /// <param name="eventArgs">Event data</param>
        private async void SendCompletedCallback(object sender, AsyncCompletedEventArgs eventArgs)
        {
            // get the event id for this asynchronous operation
            string sendEmailEventId = (string) eventArgs.UserState;
            DateTime currentDateTime = DateTime.UtcNow;

            if (eventArgs.Cancelled)
            {
                if (string.IsNullOrEmpty(sendEmailEventId))
                {
                    Console.WriteLine("[{0}] Unrecorded email send operation cancelled.", currentDateTime);
                }
                else
                {
                    Console.WriteLine("[{0}] Email send operation cancelled. Event id: [{1}]", currentDateTime, sendEmailEventId);

                    await MarkEmailRecordCancelled(sendEmailEventId, currentDateTime);
                }
            }

            if (eventArgs.Error != null)
            {
                if (string.IsNullOrEmpty(sendEmailEventId))
                {
                    Console.WriteLine("[{0}] Could not send unrecorded email, error: {1}.", currentDateTime, eventArgs.Error.ToString());
                }
                else
                {
                    Console.WriteLine("[{0}] Could not send email, error: {1}. Event id: [{2}]", currentDateTime, eventArgs.Error.ToString(), sendEmailEventId);

                    await MarkEmailRecordCancelled(sendEmailEventId, currentDateTime);
                }
            }

            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                if (string.IsNullOrEmpty(sendEmailEventId))
                {
                    Console.WriteLine("[{0}] Unrecorded email sent.", currentDateTime);
                }
                else
                {
                    Console.WriteLine("[{0}] Email sent. Event id: [{1}]", currentDateTime, sendEmailEventId);

                    await MarkEmailRecordSent(sendEmailEventId, currentDateTime);
                }
            }
        }


        /// <summary>
        ///     public void SendAsync(
        ///         string smtpFrom, string smtpTo, string subject, string body, string sendEmailEventId
        ///     )
        ///     
        ///     Sends an asynchronous email.
        /// </summary>
        /// <param name="smtpFrom">From email address</param>
        /// <param name="smtpTo">To email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="sendEmailEventId">Id for the email send event which is the email record id</param>
        public void SendAsync(string smtpFrom, string smtpTo, string subject, string body, string sendEmailEventId)
        {
            if (string.IsNullOrEmpty(sendEmailEventId))
            {
                Console.WriteLine("Sending unrecorded email.");
            }
            else
            {
                Console.WriteLine("Sending email. Event id: [{0}]", sendEmailEventId);
            }

            SmtpClient.SendAsync(smtpFrom, smtpTo, subject, body, sendEmailEventId);
        }


        /// <summary>
        ///     public void Send(string smtpFrom, string smtpTo, string subject, string body)
        ///     
        ///     Sends an email.
        /// </summary>
        /// <param name="smtpFrom">From email address</param>
        /// <param name="smtpTo">To email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        public void Send(string smtpFrom, string smtpTo, string subject, string body)
        {
            Console.WriteLine("Sending unrecorded email");

            SmtpClient.Send(smtpFrom, smtpTo, subject, body);

            Console.WriteLine("Email sent");
        }


        /// <summary>
        ///     private async Task MarkEmailRecordSent(string recordId, DateTime sentDateTime)
        ///     
        ///     Marks the email record associated with an email as sent.
        /// </summary>
        /// <param name="recordId">Id of the record</param>
        /// <param name="sentDateTime">Sent DateTime</param>
        /// <returns>void</returns>
        private async Task MarkEmailRecordSent(string recordId, DateTime sentDateTime)
        {
            using (ApplicationDbContext dbContext = new ApplicationDbContext(OptionsBuilder.Options))
            {
                EmailRecord emailRecord = await dbContext.EmailRecords.FindAsync(recordId);

                if (emailRecord != null)
                {
                    emailRecord.SetSentDateTime(sentDateTime);

                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine($"Could not mark email as sent. EmailRecord with id {recordId} not found.");
                }
            }
        }


        /// <summary>
        ///     private async Task MarkEmailRecordCancelled(string recordId, DateTime dateTime)
        ///     
        ///     Marks the email record associated with an email as cancelled.
        /// </summary>
        /// <param name="recordId">Id of the email record</param>
        /// <param name="dateTime">Cancellation DateTime</param>
        /// <returns>void</returns>
        private async Task MarkEmailRecordCancelled(string recordId, DateTime dateTime)
        {
            using (ApplicationDbContext dbContext = new ApplicationDbContext(OptionsBuilder.Options))
            {
                EmailRecord emailRecord = await dbContext.EmailRecords.FindAsync(recordId);

                if (emailRecord != null)
                {
                    emailRecord.SetCancelledDateTime(dateTime);

                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine($"Could not mark email as cancelled. EmailRecord with id {recordId} not found.");
                }
            }
        }

        public void Dispose()
        {
            // clean up
            if (SmtpClient != null)
            {
                SmtpClient.Dispose();
                SmtpClient = null;
            }
        }
    }
}
