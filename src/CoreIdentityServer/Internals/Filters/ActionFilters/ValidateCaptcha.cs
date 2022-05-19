using DNTCaptcha.Core;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Filters.ActionFilters
{
    public class ValidateCaptcha : ActionFilterAttribute
    {
        public string ErrorMessage { get; private set; } = "Please enter the security code as a number.";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ValidateDNTCaptchaAttribute captchaAttributeInstance = new ValidateDNTCaptchaAttribute
            {
                CaptchaGeneratorDisplayMode = DisplayMode.NumberToWord,
                CaptchaGeneratorLanguage = Language.English,
                ErrorMessage = ErrorMessage
            };

            captchaAttributeInstance.OnActionExecuting(context);
        }
    }
}
