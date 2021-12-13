using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing;

namespace CoreIdentityServer.Internals.Abstracts
{
    public abstract class BaseService
    {
        private protected RouteValueDictionary GenerateRedirectRouteValues(string action, string controller, string area)
        {
            return new RouteValueDictionary(
                new {
                    action,
                    controller,
                    area
                }
            );
        }

        private protected bool ValidateModel(object model)
        {
            ValidationContext validationContext = new ValidationContext(model);
            List<ValidationResult> validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults);

            return isValid;
        }
    }
}
