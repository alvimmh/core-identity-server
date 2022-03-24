// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using CoreIdentityServer.Areas.Access.Models.Consent;
using CoreIdentityServer.Internals.Extensions;
using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Internals.Constants.Authorization;

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

        [HttpGet, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> Index(string returnUrl)
        {
            ConsentViewModel viewModel = await ConsentService.ManageConsent(returnUrl);

            if (viewModel != null)
                return View("Index", viewModel);

            return View("~/Areas/ClientServices/Views/Correspondence/Error.cshtml");
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.TOTPChallenge)]
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