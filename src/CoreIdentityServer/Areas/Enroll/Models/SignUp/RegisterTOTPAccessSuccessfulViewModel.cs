namespace CoreIdentityServer.Areas.Enroll.Models.SignUp
{
    public class RegisterTOTPAccessSuccessfulViewModel
    {
        public string TOTPRecoveryCodes { get; set; }
        public bool ResetAccess { get; set; }
    }
}