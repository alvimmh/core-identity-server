using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.ActionFilters
{
    public class RedirectAuthenticatedUser : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new RedirectResult("~/Enroll/SignUp/RegisterTOTPAccessSuccessful");
            }
        }
    }
}