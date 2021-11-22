using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Services.EmailService
{
    public class EmailService : IDisposable
    {
        private IConfiguration Config;
        private string SmtpHost;
        private int SmtpPort;
        private string SmtpUsername;
        private string SmtpPassword;
        private SmtpClient SmtpClient;

        public EmailService(IConfiguration config) {
            Config = config;

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
        }

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs eventArgs)
        {
            // get the event id for this asynchronous operation
            string sendEmailEventId = (string) eventArgs.UserState;

            if (eventArgs.Cancelled)
            {
                Console.WriteLine("Email send operation cancelled. Event id: [{0}]", sendEmailEventId);
            }

            if (eventArgs.Error != null)
            {
                Console.WriteLine("Could not send email, error: {1}. Event id: [{0}]", sendEmailEventId, eventArgs.Error.ToString());
            }

            if (!eventArgs.Cancelled && eventArgs.Error == null)
            {
                Console.WriteLine("Email sent. Event id: [{0}]", sendEmailEventId);
            }
        }

        public void Send(string smtpFrom, string smtpTo, string subject, string body)
        {
            // create id for this email send event
            string sendEmailEventId = Guid.NewGuid().ToString();

            Console.WriteLine("Sending email. Event id: [{0}]", sendEmailEventId);

            SmtpClient.SendAsync(smtpFrom, smtpTo, subject, body, sendEmailEventId);
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
