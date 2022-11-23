using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CoreIdentityServer.Internals.Services.Email
{
    public class SMTPService : IDisposable
    {
        private IConfiguration Config;
        private string SmtpHost;
        private int SmtpPort;
        private string SmtpUsername;
        private string SmtpPassword;
        private SmtpClient SmtpClient;
        private DbContextOptionsBuilder<ApplicationDbContext> OptionsBuilder;

        public SMTPService(IWebHostEnvironment environment, IConfiguration config) {
            Config = config;

            bool isSmtpPortValid = false;

            // configure SMTP client
            SmtpHost = Config["smtp_host"];
            isSmtpPortValid = int.TryParse(Config["smtp_port"], out SmtpPort);
            SmtpUsername = Config["smtp_username"];
            SmtpPassword = Config["smtp_password"];

            if (
                string.IsNullOrWhiteSpace(SmtpHost) ||
                !isSmtpPortValid ||
                string.IsNullOrWhiteSpace(SmtpUsername) ||
                string.IsNullOrWhiteSpace(SmtpPassword)
            ) {
                throw new NullReferenceException("Mailtrap SMTP credentials are missing");
            }

            SmtpClient = new SmtpClient(SmtpHost, SmtpPort) {
                Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl = true
            };

            // add send completed event handler
            SmtpClient.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);

            string dbConnectionStringRoot = Config["cis_main_db_connection_string"];

            if (string.IsNullOrWhiteSpace(dbConnectionStringRoot))
                throw new NullReferenceException("Main database connection string is missing");

            // build ApplicationDbContext options for using statement
            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(
                dbConnectionStringRoot
            );

            string dbUserName = Config["cis_main_db_username"];
            string dbPassword = Config["cis_main_db_password"];

            if (string.IsNullOrWhiteSpace(dbUserName) || string.IsNullOrWhiteSpace(dbPassword))
                throw new NullReferenceException("Main database credentials are missing");

            dbConnectionBuilder.Username = dbUserName;
            dbConnectionBuilder.Password = dbPassword;

            string databaseConnectionString = dbConnectionBuilder.ConnectionString;

            OptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            OptionsBuilder.UseNpgsql(databaseConnectionString);
        }

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

        public void Send(string smtpFrom, string smtpTo, string subject, string body)
        {
            Console.WriteLine("Sending unrecorded email");

            SmtpClient.Send(smtpFrom, smtpTo, subject, body);

            Console.WriteLine("Email sent");
        }

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
