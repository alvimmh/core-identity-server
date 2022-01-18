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
        private IConfiguration Config;
        private string SmtpHost;
        private int SmtpPort;
        private string SmtpUsername;
        private string SmtpPassword;
        private SmtpClient SmtpClient;
        private DbContextOptionsBuilder<ApplicationDbContext> OptionsBuilder;

        public SMTPService(IConfiguration config) {
            Config = config;

            // configure SMTP client
            SmtpHost = Config["MailtrapSmtpEmailService:SmtpHost"];
            SmtpPort = Convert.ToInt32(Config["MailtrapSmtpEmailService:SmtpPort"]);
            SmtpUsername = Config["MailtrapSmtpEmailService:SmtpUsername"];
            SmtpPassword = Config["MailtrapSmtpEmailService:SmtpPassword"];

            SmtpClient = new SmtpClient(SmtpHost, SmtpPort) {
                Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl = true
            };

            // add send completed event handler
            SmtpClient.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);

            // build ApplicationDbContext options for using statement
            NpgsqlConnectionStringBuilder dbConnectionBuilder = new NpgsqlConnectionStringBuilder(Config.GetConnectionString("MainDatabaseConnection"));

            dbConnectionBuilder.Username = Config["cisdb_username"];
            dbConnectionBuilder.Password = Config["cisdb_password"];

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
                Console.WriteLine("[{0}] Email send operation cancelled. Event id: [{1}]", currentDateTime, sendEmailEventId);
                await MarkEmailRecordCancelled(sendEmailEventId, currentDateTime);
            }

            if (eventArgs.Error != null)
            {
                Console.WriteLine("[{0}] Could not send email, error: {1}. Event id: [{2}]", currentDateTime, eventArgs.Error.ToString(), sendEmailEventId);
                await MarkEmailRecordCancelled(sendEmailEventId, currentDateTime);
            }

            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                Console.WriteLine("[{0}] Email sent. Event id: [{1}]", currentDateTime, sendEmailEventId);

                await MarkEmailRecordSent(sendEmailEventId, currentDateTime);
            }
        }

        public void Send(string smtpFrom, string smtpTo, string subject, string body, string emailId)
        {
            // create id for this email send event
            string sendEmailEventId = emailId;

            Console.WriteLine("Sending email. Event id: [{0}]", sendEmailEventId);

            SmtpClient.SendAsync(smtpFrom, smtpTo, subject, body, sendEmailEventId);
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
