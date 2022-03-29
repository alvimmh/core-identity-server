using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace CoreIdentityServer.Areas.Administration.Models.Roles
{
    public class IndexViewModel
    {
        public List<IdentityRole> Roles { get; set; }
    }
}
