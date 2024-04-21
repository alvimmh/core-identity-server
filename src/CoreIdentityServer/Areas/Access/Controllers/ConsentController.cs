// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Filters.ResultFilters;
using CoreIdentityServer.Areas.Access.Models.Consent;
using CoreIdentityServer.Internals.Extensions;
using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Internals.Constants.Routing;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.Access), SecurityHeaders, Authorize]
    public class ConsentController : Controller
    {
        private ConsentService ConsentService;

        public ConsentController(ConsentService consentService)
        {
            ConsentService = consentService;
        }


        /// The HTTP GET action to show the Consent Controller's Index page
        [HttpGet]
        public async Task<IActionResult> Index(string returnUrl)
        {
            ConsentViewModel viewModel = await ConsentService.ManageConsent(returnUrl);

            if (viewModel != null)
                return View("Index", viewModel);

            return View("~/Areas/ClientServices/Views/Correspondence/Error.cshtml");
        }


        /// The HTTP POST action from the Consent Controller's Index page
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConsentInputModel inputModel)
        {
            object[] consentResponse = await ConsentService.ManageConsentResponse(inputModel);
            ProcessConsentResult consentResult = (ProcessConsentResult)consentResponse[0];
            bool nativeRedirect = (bool)consentResponse[1];

            if (consentResult.IsRedirect)
            {
                if (nativeRedirect)
                    return this.LoadingPage("Redirect", consentResult.RedirectUri);

                return Redirect(consentResult.RedirectUri);
            }

            if (consentResult.ShowView)
            {
                return View("Index", consentResult.ViewModel);
            }

            return View("~/Areas/ClientServices/Views/Correspondence/Error.cshtml");
        }
    }
}