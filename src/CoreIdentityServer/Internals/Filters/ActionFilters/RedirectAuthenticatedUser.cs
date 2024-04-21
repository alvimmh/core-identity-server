using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.ActionFilters
{
    // filter to redirect authenticated users to the Dashboard page before the action executes
    public class RedirectAuthenticatedUser : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new RedirectResult("~/general/pages/dashboard");
            }
        }
    }
}
