// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CoreIdentityServer.Internals.Filters.ActionFilters;
using CoreIdentityServer.Internals.Constants.Routes;
using CoreIdentityServer.Areas.Access.Services;
using CoreIdentityServer.Areas.Access.Models.Grants;

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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            GrantsViewModel viewModel = await GrantsService.ManageGrants();

            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(string clientId)
        {
            await GrantsService.RevokeGrant(clientId, User);

            return RedirectToAction("Index");
        }
    }
}