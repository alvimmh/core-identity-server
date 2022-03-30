using System;

namespace CoreIdentityServer.Internals.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public Func<int, string> MethodToGeneratePageLink { get; set; }

        public string CSSClassForActiveStatus(int pageNumber)
        {
            return pageNumber == CurrentPage ? "active" : null;
        }

        public string PageLink(int pageNumber)
        {
            return MethodToGeneratePageLink(pageNumber);
        }
    }
}
