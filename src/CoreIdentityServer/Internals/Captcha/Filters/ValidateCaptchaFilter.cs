using System.Net;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Captcha.Constants;
using CoreIdentityServer.Internals.Constants.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoreIdentityServer.Internals.Captcha.Filters
{
    // Asynchronous action filter to validate captcha
    public class ValidateCaptchaFilter : IAsyncActionFilter
    {
        public string ErrorMessage { get; private set; }
        private readonly CloudflareCaptchaProvider CloudflareCaptchaProvider;

        public ValidateCaptchaFilter(CloudflareCaptchaProvider cloudflareCaptchaProvider)
        {
            CloudflareCaptchaProvider = cloudflareCaptchaProvider;
            ErrorMessage = "Captcha validation failed. Please try again.";
        }

        /// <summary>
        ///     public async Task OnActionExecutionAsync(
        ///         ActionExecutingContext context, ActionExecutionDelegate next
        ///     )
        ///     
        ///     Filter to verify captcha response before action executes.
        ///     
        ///     1. Gets the captcha response value and the user's IP address from the HTTPContext.
        ///     
        ///     2. Verifies the captcha response using the CloudflareCaptchaProvider.Verify() method.
        ///     
        ///     3. If verification fails, an error message is added in the TempData for the user, and
        ///         the user is redirected back to the page where they failed the captcha challenge.
        ///         
        ///     4. If verification passes, the user is allowed to proceed.
        /// </summary>
        /// <param name="context">Action filter context</param>
        /// <param name="next">
        ///     A delegate invoked to execute the next action filter or the action itself.
        /// </param>
        /// <returns>void</returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string captchaResponseDataName = GeneralConstants.CaptchaResponseFormDataName;

            string captchaResponse = context.HttpContext.Request.Form[captchaResponseDataName];
            IPAddress userIPAddress = context.HttpContext.Connection.RemoteIpAddress;

            bool isCaptchaVerified = await CloudflareCaptchaProvider.Verify(captchaResponse, userIPAddress);

            if (!isCaptchaVerified)
            {
                ((Controller)context.Controller).TempData.Add(TempDataKeys.ErrorMessage, ErrorMessage);

                string currentRoute = context.HttpContext.Request.Path;

                context.Result = new RedirectResult(currentRoute);
            }
            else
            {
                await next();
            }
        }
    }
}
