using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.ClientServices.Models.Correspondence
{
    public class ResendEmailInputModel
    {
        [Required]
        public string ResendEmailRecordId { get; set; }

        public string ResendEmailErrorMessage { get; private set; }

        public void SetErrorMessage(string message)
        {
            ResendEmailErrorMessage = message;
        }
    }
}