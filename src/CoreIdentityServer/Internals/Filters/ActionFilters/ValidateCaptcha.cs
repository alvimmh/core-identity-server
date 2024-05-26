using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.ActionFilters
{
    public class ValidateCaptcha : ActionFilterAttribute
    {
        public string ErrorMessage { get; private set; }


        // filter to validate captcha before action executes
        public override void OnActionExecuting(ActionExecutingContext context)
        {}
    }
}
