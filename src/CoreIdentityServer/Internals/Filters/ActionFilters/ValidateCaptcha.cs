using DNTCaptcha.Core;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.ActionFilters
{
    public class ValidateCaptcha : ActionFilterAttribute
    {
        public string ErrorMessage { get; private set; } = "Please enter the captcha code as a number.";

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
