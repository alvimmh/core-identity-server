using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace CoreIdentityServer.Services.EmailService
{
    public class EmailService
    {
        private IConfiguration Config;
        private string SmtpHost;
        private int SmtpPort;
        private string SmtpUsername;
        private string SmtpPassword;


        public EmailService(IConfiguration config) {
            Config = config;

            SmtpHost = Config["MailtrapSmtpEmailService:SmtpHost"];
            SmtpPort = Convert.ToInt32(Config["MailtrapSmtpEmailService:SmtpPort"]);
            SmtpUsername = Config["MailtrapSmtpEmailService:SmtpUsername"];
            SmtpPassword = Config["MailtrapSmtpEmailService:SmtpPassword"];
        }

        private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs eventArgs)
        {
            // get the source SmtpClient which emitted this email send event
            SmtpClient sourceSmtpClient = (SmtpClient) sender;
            
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

            // clean up
            sourceSmtpClient.Dispose();
        }

        public void Send(string smtpFrom, string smtpTo, string subject, string body)
        {
            // create id for this email send event
            string sendEmailEventId = Guid.NewGuid().ToString();

            SmtpClient SmtpClient = new SmtpClient(SmtpHost, SmtpPort) {
                Credentials = new NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl = true
            };

            Console.WriteLine("Initiating email send event. Event id: [{0}]", sendEmailEventId);

            SmtpClient.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
            SmtpClient.SendAsync(smtpFrom, smtpTo, subject, body, sendEmailEventId);
        }
    }
}
