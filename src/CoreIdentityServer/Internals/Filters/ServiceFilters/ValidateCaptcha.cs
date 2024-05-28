using CoreIdentityServer.Internals.Captcha.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CoreIdentityServer.Internals.Filters.ServiceFilters
{
    // Service filter that makes use of Dependency Injection to
    // resolve dependencies for the underlying ValidateCaptchaFilter 
    public class ValidateCaptcha : ServiceFilterAttribute
    {
        public ValidateCaptcha() : base(typeof(ValidateCaptchaFilter))
        {}
    }
}