// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using CoreIdentityServer.Internals.Constants.Routes;
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

        [HttpGet, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> Index()
        {
            GrantsViewModel viewModel = await GrantsService.ManageGrants();

            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = Policies.TOTPChallenge)]
        public async Task<IActionResult> Index([FromForm] RevokeGrantInputModel inputModel)
        {
            await GrantsService.RevokeGrant(inputModel);

            return RedirectToAction("Index");
        }
    }
}