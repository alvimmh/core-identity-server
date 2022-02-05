namespace CoreIdentityServer.Areas.Access.Models.Authentication
{
    public class SignedOutViewModel
    {
        public string PostLogoutRedirectUri { get; set; }
        public string ClientName { get; set; }
        public string SignOutIFrameUrl { get; set; }
        public bool AutomaticRedirectAfterSignOut { get; set; }
        public string SignOutId { get; set; }
    }
}
