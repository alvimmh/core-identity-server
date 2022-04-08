namespace CoreIdentityServer.Areas.Administration.Models.Users
{
    public class SearchUsersInputModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Page { get; set; }
    }
}
