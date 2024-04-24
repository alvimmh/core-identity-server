// Copyright (c) Duende Software. All rights reserved.
//
// By accessing the Duende IdentityServer code here, you are agreeing to the following licensing terms:
// https://duendesoftware.com/license/identityserver.pdf
//
// or, alternatively you can view the license in the project's ~/Licenses/DuendeSoftwareLicense.pdf file
//
// If you do not agree to these terms, do not access the Duende IdentityServer code.


using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Internals.Filters.ResultFilters;
using CoreIdentityServer.Internals.Constants.Routing;
using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Areas.Access.Models.Grants;
using CoreIdentityServer.Internals.Constants.Authorization;

namespace CoreIdentityServer.Areas.Access.Controllers
{
    [Area(AreaNames.Access), SecurityHeaders, Authorize]
    public class GrantsController : Controller
    {
        private GrantsService GrantsService;

        public GrantsController(GrantsService grantsService)
        {
            GrantsService = grantsService;
        }


        /// The HTTP GET action to show the Grant's Controller's Index page
        [HttpGet, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> Index()
        {
            GrantsViewModel viewModel = await GrantsService.ManageGrants();

            return View(viewModel);
        }


        /// The HTTP POST action from the Grant's Controller's Index page
        /// Used to revoke grants.
        public async Task<IActionResult> Index([FromForm] RevokeGrantInputModel inputModel)
        {
            await GrantsService.RevokeGrant(inputModel);

            return RedirectToAction("Index");
        }
    }
}