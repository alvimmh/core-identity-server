using System.Collections.Generic;

namespace CoreIdentityServer.Areas.Administration.Models.Users
{
    public class IndexViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int ResultsInPage { get; private set; } = 15;
        public List<UserViewModel> Users;
    }
}
