using System;
using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Internals.Models.DatabaseModels
{
    public class EmailRecord
    {
        public string Id { get; private set; }

        [EmailAddress]
        public string SentFrom { get; private set; }

        [EmailAddress]
        public string SentTo { get; private set; }

        public string Subject { get; private set; }
        public string Body { get; private set; }
        public DateTime? SentAt { get; private set; }
        public string ResentAt { get; private set; }
        public string CancelledAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public EmailRecord()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }

        public void SetRecordDetails(string sentFrom, string sentTo, string subject, string body, DateTime sentAtUTC)
        {
            SentFrom = sentFrom;
            SentTo = sentTo;
            Subject = subject;
            Body = body;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetSentDateTime(DateTime dateTime)
        {           
            if (SentAt == null)
            {
                SentAt = dateTime;
            }
            else
            {
                string sentDateTimeString = dateTime.ToString();
                ResentAt = ResentAt == null ? sentDateTimeString : $"{ResentAt},{sentDateTimeString}";
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void SetCancelledDateTime(DateTime dateTime)
        {
            string cancelledDateTimeString = dateTime.ToString();
            CancelledAt = CancelledAt == null ? cancelledDateTimeString : $"{CancelledAt},{cancelledDateTimeString}";

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
