using System;
using System.ComponentModel.DataAnnotations;
using CoreIdentityServer.Areas.ClientServices.Models.Correspondence;

namespace CoreIdentityServer.Internals.Models.DatabaseModels
{
    // Class representing records of emails sent to users
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
        public int SendAttempts { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public EmailRecord()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }

        // Sets the details for the email record
        public void SetRecordDetails(string sentFrom, string sentTo, string subject, string body, DateTime sentAtUTC)
        {
            SentFrom = sentFrom;
            SentTo = sentTo;
            Subject = subject;
            Body = body;
            UpdatedAt = DateTime.UtcNow;
        }

        // Sets the send/resend DateTime for the email record
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

            SendAttempts++;
            UpdatedAt = DateTime.UtcNow;
        }

        // Sets the cancel DateTime for the email record
        public void SetCancelledDateTime(DateTime dateTime)
        {
            string cancelledDateTimeString = dateTime.ToString();
            CancelledAt = CancelledAt == null ? cancelledDateTimeString : $"{CancelledAt},{cancelledDateTimeString}";

            SendAttempts++;
            UpdatedAt = DateTime.UtcNow;
        }

        // Determines if the email associated with the record can be resent
        public bool CanResendEmail(ResendEmailInputModel inputModel)
        {
            if (SentTo != inputModel.Email)
            {
                inputModel.SetErrorMessage("Could not resend email");

                return false;
            }

            DateTime currentDateTime = DateTime.UtcNow;
            bool emailExpired = currentDateTime - CreatedAt > TimeSpan.FromMinutes(5);

            if (emailExpired || SendAttempts >= 5)
            {
                inputModel.SetErrorMessage("Resend blocked");

                return false;
            }

            DateTime? emailCreatedAt = (DateTime?)CreatedAt;

            bool emailPreviouslyCancelled = CancelledAt != null;
            string[] allCancellations = null;
            DateTime? lastCancelledAt = null;

            if (emailPreviouslyCancelled)
            {
                allCancellations = CancelledAt.Split(',', 5);
                lastCancelledAt = DateTime.Parse(allCancellations[allCancellations.Length - 1]);
            }

            bool emailPreviouslySent = SentAt != null;

            bool emailPreviouslyResent = ResentAt != null;
            string[] allResends = null;
            DateTime? lastResentAt = null;

            if (emailPreviouslyResent)
            {
                allResends = ResentAt.Split(',', 5);
                lastResentAt = DateTime.Parse(allResends[allResends.Length - 1]);
            }

            DateTime? firstPairLatest = emailCreatedAt;
            DateTime? secondPairLatest = SentAt;
            DateTime? latestAttemptAt = null;

            if (lastCancelledAt != null)
                firstPairLatest = emailCreatedAt > lastCancelledAt ? emailCreatedAt : lastCancelledAt;
            
            if (emailPreviouslyResent)
                secondPairLatest = lastResentAt;

            latestAttemptAt = firstPairLatest > secondPairLatest ? firstPairLatest : secondPairLatest;

            bool shouldThrottleEmail = currentDateTime - latestAttemptAt < TimeSpan.FromSeconds(30);

            if (shouldThrottleEmail)
            {
                inputModel.SetErrorMessage("Please try again in 30 seconds");

                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
