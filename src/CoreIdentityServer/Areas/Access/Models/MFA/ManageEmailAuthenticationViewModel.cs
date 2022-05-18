namespace CoreIdentityServer.Areas.Access.Models.MFA
{
    public class ManageEmailAuthenticationViewModel : SetEmailAuthenticationInputModel
    {
        public bool EmailAuthenticationEnabled { get; private set; }

        public void SetEmailAuthenticationEnabled(bool enabled)
        {
            EmailAuthenticationEnabled = enabled;
        }
    }
}
