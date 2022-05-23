using System.ComponentModel.DataAnnotations;

namespace CoreIdentityServer.Areas.Access.Models.ResetTOTPAccess
{
    public class ManageAuthenticatorViewModel
    {
        public int RecoveryCodesLeft { get; set; }
    }
}
