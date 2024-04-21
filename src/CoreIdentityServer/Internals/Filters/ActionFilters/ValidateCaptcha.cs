using DNTCaptcha.Core;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.ActionFilters
{
    public class ValidateCaptcha : ActionFilterAttribute
    {
        public string ErrorMessage { get; private set; } = "Please enter the number shown in the captcha";


        // filter to validate captcha before action executes
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ValidateDNTCaptchaAttribute captchaAttributeInstance = new ValidateDNTCaptchaAttribute
            {
                ErrorMessage = ErrorMessage
            };

            captchaAttributeInstance.OnActionExecuting(context);
        }
    }
}
