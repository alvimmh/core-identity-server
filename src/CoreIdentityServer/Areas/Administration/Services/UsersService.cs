using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CoreIdentityServer.Areas.Administration.Models.Users;
using CoreIdentityServer.Internals.Constants.Administration;
using CoreIdentityServer.Internals.Constants.Errors;
using CoreIdentityServer.Internals.Constants.Storage;
using CoreIdentityServer.Internals.Data;
using CoreIdentityServer.Internals.Models.DatabaseModels;
using CoreIdentityServer.Internals.Services;
using CoreIdentityServer.Internals.Services.BackChannelCommunications;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoreIdentityServer.Areas.Administration.Services
{
    public class UsersService : BaseService, IDisposable
    {
        private readonly ApplicationDbContext DbContext;
        private readonly UserManager<ApplicationUser> UserManager;
        private BackChannelNotificationService BackChannelNotificationService;
        private IUrlHelper UrlHelper;
        public readonly string RootRoute;
        private bool ResourcesDisposed;

        public UsersService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            BackChannelNotificationService backChannelNotificationService,
            IActionContextAccessor actionContextAccessor,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IUrlHelperFactory urlHelperFactory
        ) : base(actionContextAccessor, tempDataDictionaryFactory)
        {
            DbContext = dbContext;
            UserManager = userManager;
            BackChannelNotificationService = backChannelNotificationService;
            UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            RootRoute = GenerateRouteUrl("Index", "Users", "Administration");
        }


        /// <summary>
        ///     public async Task<IndexViewModel> ManageIndex(string page)
        ///     
        ///     Manages the Index action to show all users.
        ///     
        ///     1. Counts all users of this application. Then calculates the total
        ///         pages required to view all users, each page containing a number
        ///             of viewModel.ResultsInPage users.
        ///             
        ///     2. Fetches users ordered by the CreatedAt DateTime, then skips a
        ///         number of users in case the request is for a specific page,
        ///             and takes viewModel.ResultsInPage number of users after the
        ///                 last skipped user. Finally, the result is added to the
        ///                     view model and the method returns this view model.
        /// </summary>
        /// <param name="page">Current page number if the request was for a specific page</param>
        /// <returns>View model containing a list of users to show in the current page</returns>
        public async Task<IndexViewModel> ManageIndex(string page)
        {
            IndexViewModel viewModel = new IndexViewModel();

            int totalUsers = await UserManager.Users.CountAsync();
            viewModel.TotalResults = totalUsers;
            
            double totalPages = (double)totalUsers / viewModel.ResultsInPage;
            viewModel.TotalPages = (int)Math.Ceiling(totalPages);

            int skipRecords = 0;

            bool isQueryStringInputValid = Int32.TryParse(page, out int currentPage);

            if (isQueryStringInputValid && currentPage > 0 && currentPage <= viewModel.TotalPages)
            {
                viewModel.CurrentPage = currentPage;
                int lastPage = currentPage - 1;
                skipRecords = lastPage * viewModel.ResultsInPage;
            }
            else
            {
                viewModel.CurrentPage = 1;
                skipRecords = 0;
            }

            viewModel.Users = await UserManager.Users
                                                .OrderBy(item => item.CreatedAt)
                                                .Skip(skipRecords)
                                                .Take(viewModel.ResultsInPage)
                                                .ProjectToType<UserViewModel>()
                                                .ToListAsync();

            return viewModel;
        }


        /// <summary>
        ///     public async Task<IndexViewModel> ManageSearch(SearchUsersInputModel inputModel)
        ///
        ///     Manages the Search action to find users.
        ///     
        ///     1. Checks if all search parameters are empty. If they are empty, an error
        ///         message is added to the ModelState and the method returns the view model
        ///             with an empty list of users.
        ///             
        ///     2. If the search was done by id, the user is fetched using the method
        ///         UserManager.FindByIdAsync(). If the user is found, the user is added
        ///             to the searchResults array and the method returns a view model
        ///                 containing this array.
        ///             
        ///     3. If the search was done by email, the user is fetched using the
        ///         method UserManager.FindByEmailAsync(). If the user is found, the
        ///             user is added to the searchResults array and the method returns
        ///                 this array as part of the view model.
        ///                 
        ///     4. If the search was done by either the first or last name, a search filter
        ///         is created for the database query using the method GenerateNamesFilter().
        ///             Using this filter, the search result is procured and paginated before
        ///                 adding to the view model. Finally, the method returns the view model
        ///                     containing the result.
        ///                 
        ///     5. In case no users were found for a given search parameter, an empty view
        ///         model is returned from the method.
        /// </summary>
        /// <param name="inputModel">
        ///     Input model containing search parameters and pagination data
        /// </param>
        /// <returns>
        ///     View model containing the search result and pagination data
        /// </returns>
        public async Task<IndexViewModel> ManageSearch(SearchUsersInputModel inputModel)
        {
            List<UserViewModel> searchResults = new List<UserViewModel>();

            bool idEmpty = string.IsNullOrWhiteSpace(inputModel.Id);
            bool emailEmpty = string.IsNullOrWhiteSpace(inputModel.Email);
            bool firstNameEmpty = string.IsNullOrWhiteSpace(inputModel.FirstName);
            bool lastNameEmpty = string.IsNullOrWhiteSpace(inputModel.LastName);

            if (idEmpty && emailEmpty && firstNameEmpty && lastNameEmpty)
            {
                ActionContext.ModelState.AddModelError(string.Empty, "Please specify a search parameter.");

                return new IndexViewModel { Users = searchResults };
            }

            if (!idEmpty)
            {
                ApplicationUser user = await UserManager.FindByIdAsync(inputModel.Id);

                if (user != null)
                    searchResults.Add(user.Adapt<UserViewModel>());

                return new IndexViewModel { Id = inputModel.Id, Users = searchResults, TotalResults = searchResults.Count };
            }

            if (!emailEmpty)
            {
                ApplicationUser user = await UserManager.FindByEmailAsync(inputModel.Email);

                if (user != null)
                    searchResults.Add(user.Adapt<UserViewModel>());

                return new IndexViewModel { Email = inputModel.Email, Users = searchResults, TotalResults = searchResults.Count };
            }

            if (!firstNameEmpty || !lastNameEmpty)
            {
                IndexViewModel viewModel = new IndexViewModel() {
                    FirstName = inputModel.FirstName,
                    LastName = inputModel.LastName
                };

                Expression<Func<ApplicationUser, bool>> searchFilter = GenerateNamesFilter(inputModel.FirstName, inputModel.LastName, firstNameEmpty, lastNameEmpty);

                int totalMatchedUsers = await UserManager.Users.Where(searchFilter).CountAsync();

                double totalPages = (double)totalMatchedUsers / viewModel.ResultsInPage;
                viewModel.TotalPages = (int)Math.Ceiling(totalPages);
                viewModel.TotalResults = totalMatchedUsers;

                int skipRecords = 0;

                bool isPageNumberInputValid = Int32.TryParse(inputModel.Page, out int currentPage);

                if (isPageNumberInputValid && currentPage > 0 && currentPage <= viewModel.TotalPages)
                {
                    viewModel.CurrentPage = currentPage;
                    int lastPage = currentPage - 1;
                    skipRecords = lastPage * viewModel.ResultsInPage;
                }
                else
                {
                    viewModel.CurrentPage = 1;
                    skipRecords = 0;
                }

                searchResults = await UserManager.Users
                                                    .Where(searchFilter)
                                                    .OrderBy(item => item.CreatedAt)
                                                    .Skip(skipRecords)
                                                    .Take(viewModel.ResultsInPage)
                                                    .ProjectToType<UserViewModel>()
                                                    .ToListAsync();

                viewModel.Users = searchResults;

                return viewModel;
            }

            return new IndexViewModel() { Users = searchResults };
        }


        /// <summary>
        ///     public async Task<object[]> ManageDetails(string userId)
        ///     
        ///     Manages the Details action.
        ///     
        ///     1. Searches the user by the userId param. If the user is not
        ///         found, the method returns an array of objects containing
        ///             null and the RootRoute.
        ///             
        ///     2. If the user is found, the current administrative user's action
        ///         of accessing this user's details is recorded using the method
        ///             RecordUserAccess().
        ///             
        ///     3. If the user access recording is successful, the method creates
        ///         a view model with the user's data and returns an array of
        ///             objects containing the created view model and null.
        ///             
        ///     4. In case the user access recording failed, an error message is
        ///         added for the administrative user and the method returns an
        ///             array of objects containing null and the RootRoute.
        /// </summary>
        /// <param name="userId">The id of the user whose details is being accessed</param>
        /// <returns>
        ///     An array of objects containing
        ///         the view model and null
        ///             or,
        ///                 null and the RootRoute.
        /// </returns>
        public async Task<object[]> ManageDetails(string userId)
        {
            ApplicationUser user = await UserManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found.";

                return GenerateArray(null, RootRoute);
            }
            else
            {
                bool userAccessRecorded = await RecordUserAccess(user, UserAccessPurposes.Details);

                if (userAccessRecorded)
                {
                    UserDetailsViewModel viewModel = user.Adapt<UserDetailsViewModel>();

                    return GenerateArray(viewModel, null);
                }
                else
                {
                    TempData[TempDataKeys.ErrorMessage] = "Could not access user. Please try again.";

                    return GenerateArray(null, RootRoute);
                }
            }
        }


        /// <summary>
        ///     public async Task<object[]> ManageEdit(string userId)
        ///     
        ///     Manages the Edit action to show the edit page which
        ///         contains sensitive data of a user.
        ///         
        ///     1. Finds the user by the userId param. If the user is not found,
        ///         the method returns an array of objects containing null and
        ///             the RootRoute.
        ///             
        ///     2. If the user is found, the action of the administrative user
        ///         accessing this user's data is recorded using the method
        ///             RecordUserAccess(). If the recording fails, the method
        ///                 returns an array of objects containing null and the
        ///                     RootRoute.
        ///                     
        ///     3. If the user access is recorded, the method returns a view model
        ///         containing data about the user.
        /// </summary>
        /// <param name="userId">Id of the user whose information is being accessed</param>
        /// <returns>
        ///     An array of objects containing
        ///         the view model and null
        ///             or,
        ///                 null and the RootRoute.
        /// </returns>
        public async Task<object[]> ManageEdit(string userId)
        {
            ApplicationUser user = await UserManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found.";

                return GenerateArray(null, RootRoute);
            }
            else
            {
                bool userAccessRecorded = await RecordUserAccess(user, UserAccessPurposes.Edit);

                if (!userAccessRecorded)
                {
                    TempData[TempDataKeys.ErrorMessage] = "Could not access user. Please try again.";

                    return GenerateArray(null, RootRoute);
                }
                else
                {
                    EditUserInputModel viewModel = user.Adapt<EditUserInputModel>();

                    return GenerateArray(viewModel, RootRoute);
                }
            }
        }


        /// <summary>
        ///     public async Task<string> ManageUpdate(EditUserInputModel inputModel)
        ///     
        ///     Manages the Edit action to update a user's details.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns null.
        ///     
        ///     2. Fetches the user by the id. If the user is not found, the method
        ///         returns the RootRoute.
        ///         
        ///     3. If the user is found and the user's first name and last name are
        ///         the same as the ones found in the input model, the method returns
        ///             null as the user's details are already up to date.
        ///             
        ///     4. Otherwise, a database transaction is started.
        ///     
        ///        In this transaction, the action of the administrative user's accessing
        ///         the user's data is recorded first. If the recording fails, the
        ///             transaction is rolled back, an error message is added for the
        ///                 administrative user and the method returns null.
        ///                 
        ///        If the recording succeeded, the user is then updated. If the udpate fails,
        ///         the transaction is rolled back and any errors are added to the ModelState.
        ///             Then the method returns null.
        ///             
        ///        If the update succeeded, the transaction is committed and the method returns
        ///         a url to the user's details page.
        /// </summary>
        /// <param name="inputModel">
        ///     Input model containing user's data that needs to be changed
        /// </param>
        /// <returns>A url to the user's details page or null</returns>
        public async Task<string> ManageUpdate(EditUserInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
                return null;
            
            ApplicationUser user = await UserManager.FindByIdAsync(inputModel.Id);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found";

                return RootRoute;
            }
            else if (user.FirstName == inputModel.FirstName && user.LastName == inputModel.LastName)
            {
                TempData[TempDataKeys.ErrorMessage] = "Edit fields to update user.";

                return null;
            }
            else
            {
                user.FirstName = inputModel.FirstName;
                user.LastName = inputModel.LastName;

                using IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync();

                bool userAccessRecorded = await RecordUserAccess(user, UserAccessPurposes.Update);

                if (!userAccessRecorded)
                {
                    await transaction.RollbackAsync();

                    TempData[TempDataKeys.ErrorMessage] = "Could not update user. Please try again.";

                    return null;
                }
                else
                {
                    IdentityResult updateUser = await UserManager.UpdateAsync(user);

                    if (!updateUser.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        // updating user failed, adding erros to ModelState
                        foreach (IdentityError error in updateUser.Errors)
                            ActionContext.ModelState.AddModelError(string.Empty, error.Description);

                        return null;
                    }
                    else
                    {
                        await transaction.CommitAsync();

                        TempData[TempDataKeys.SuccessMessage] = "User updated.";

                        return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                    }
                }
            }
        }


        /// <summary>
        ///     public async Task<string> ManageBlock(
        ///         BlockUserInputModel inputModel, bool blockUser
        ///     )
        ///     
        ///     Manages the Block and Unblock actions.
        ///     
        ///     1. Checks if the ModelState is valid. If not, the method returns
        ///         the RootRoute.
        ///         
        ///     2. Finds the user by the id. If the user is not found, the method
        ///         returns the RootRoute.
        ///         
        ///     3. Checks if the user is the Product Owner. If so, the method
        ///         returns a url to the user's details page because the Product
        ///             Owner cannot be blocked.
        ///             
        ///     4. Updates the block status of the user according to the blockUser
        ///         boolean.
        ///         
        ///     5. Starts a transaction to commit further changes.
        ///     
        ///     6. Records the action of the administrative user blocking/unblocking
        ///         the user using the RecordUserAccess() method.
        ///         
        ///        If the recording fails, the transaction is rolled back, an error
        ///         message is added for the administrative user and the method returns
        ///             a url for the user's details page.
        ///                     
        ///     7. If the recording succeeds, the details are updated in order
        ///         to block/unblock the user.
        ///         
        ///        If the update fails, the transaction is rolled back and all errors are
        ///         printed to the console. An error message is added for the administrative
        ///             user. And the method returns a url to the user's details page.
        ///             
        ///     8. If the update succeeded, the transaction is committed. A back-channel
        ///         notification to all identity server clients is sent so they can logout
        ///             the user from their end. This is done using the method
        ///                 BackChannelNotificationService.SendBackChannelLogoutNotificationsForUserAsync().
        ///                     Finally, the method returns a url to the user's details page.
        /// </summary>
        /// <param name="inputModel">
        ///     The input model containing the user's id who will be blocked/unblocked
        /// </param>
        /// <param name="blockUser">
        ///     Boolean indicating if the user will be blocked or unblocked
        /// </param>
        /// <returns>
        ///     A url to the RootRoute or the user's details page
        /// </returns>
        public async Task<string> ManageBlock(BlockUserInputModel inputModel, bool blockUser)
        {
            if (!ActionContext.ModelState.IsValid)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found.";

                return RootRoute;
            }

            ApplicationUser user = await UserManager.FindByIdAsync(inputModel.Id);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found";

                return RootRoute;
            }
            else
            {
                string blockAction = blockUser ? UserAccessPurposes.Block : UserAccessPurposes.Unblock;
                string blockActionLowerCase = blockAction.ToLower();

                bool userIsProductOwner = await UserManager.IsInRoleAsync(user, AuthorizedRoles.ProductOwner);

                if (userIsProductOwner)
                {
                    TempData[TempDataKeys.ErrorMessage] = $"Cannot {blockActionLowerCase} a user with role {AuthorizedRoles.ProductOwner}.";

                    return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                }

                user.SetBlock(blockUser);

                using IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync();

                bool userAccessRecorded = await RecordUserAccess(user, blockAction);

                if (!userAccessRecorded)
                {
                    await transaction.RollbackAsync();

                    TempData[TempDataKeys.ErrorMessage] = $"Could not {blockActionLowerCase} user. Please try again.";

                    return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                }
                else
                {
                    IdentityResult updateBlockedStatus = null;
                    
                    if (blockUser)
                    {
                        // passing the method with a changed user object saves this change
                        // when updating the security stamp of the user
                        // more information:
                        // https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/UserManager.cs#L844
                        updateBlockedStatus = await UserManager.UpdateSecurityStampAsync(user);
                    }
                    else
                    {
                        // update blocked status
                        updateBlockedStatus = await UserManager.UpdateAsync(user);
                    }

                    if (!updateBlockedStatus.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        // updating user failed, adding errors to ModelState
                        foreach (IdentityError error in updateBlockedStatus.Errors)
                            Console.WriteLine(error.Description);

                        TempData[TempDataKeys.ErrorMessage] = $"Could not {blockActionLowerCase} user. Please try again.";

                        return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                    }
                    else
                    {
                        await transaction.CommitAsync();

                        if (blockUser)
                        {
                            // send notifications to all clients of CIS to signout the user
                            await BackChannelNotificationService.SendBackChannelLogoutNotificationsForUserAsync(user);
                        }

                        TempData[TempDataKeys.SuccessMessage] = $"User {blockActionLowerCase}ed.";

                        return UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });
                    }
                }
                
            }
        }


        /// <summary>
        ///     public async Task<string> ManageDelete(DeleteUserInputModel inputModel)
        ///     
        ///     Manages the Delete action to delete a user.
        ///     
        ///     1. Checks if the ModelState is valid. If not, returns the RootRoute.
        ///     
        ///     2. Finds the user by the id. If the user is not found, the method returns
        ///         RootRoute.
        ///         
        ///     3. If the user is found, the method checks if the user is a Product Owner.
        ///         If the user is a Product Owner, the method returns a url of the user
        ///             details page.
        ///             
        ///     4. Starts a database transaction.
        ///     
        ///     5. Records the administrative user's action of 'archiving' the user using the
        ///         method RecordUserAccess(). If the recording fails, the transaction is
        ///             rolled back and the method returns a url of the user details page.
        ///             
        ///     6. If the recording succeeds, the user is archived (soft-deleted) and the
        ///         user's security stamp is updated to logout the user from all logged in
        ///             instances.
        ///             
        ///     7. If this update fails, the transaction is rolled back and
        ///                 all errors are printed to the console. An error message is added
        ///                     for the administrative user and the method returns a url of
        ///                         the user details page.
        ///                         
        ///     8. If the update succeeds, a save point is created in the transaction. This
        ///         helps if hard deletion of the user fails, the user will still remain soft
        ///             deleted.
        ///             
        ///     9. Records the action of the administrative user 'deleting' the user. If the
        ///         recording fails, the transaction is rolled back to the previous save point.
        ///             An error message is added for the administrative user and the method
        ///                 returns a url of the user details page.
        ///                 
        ///     10. If the recording succeeds, the user is deleted using the method
        ///             UserManager.DeleteAsync(). If the deletion fails, the transaction is
        ///                 rolled back to the previous save point. All errors are printed
        ///                     to the console and an error message is added for the
        ///                         administrative user. Then the method returns a url for
        ///                             the user details page.
        ///                             
        ///     11. If the deletion is successful, a back-channel delete notification is sent
        ///         to all identity server clients so they delete the user on their ends. The
        ///             method BackChannelNotificationService.SendBackChannelDeleteNotificationsForUserAsync()
        ///                 is used to send this notification.
        ///                 
        ///     12. If deleting the user from the identity server clients failed, and there
        ///         were errors reported from the clients, the transaction is rolled back to the
        ///             previous save point. And this save point is committed. A back-channel
        ///                 logout notification is sent to all identity server clients so they
        ///                     logout the user on their ends. An error message is added for the
        ///                         administrative user. And the method returns the url for the
        ///                             user details page.
        ///                         
        ///         In case there were no errors in from the identity server after trying to
        ///             delete the user from the clients, but the deletion failed from their ends
        ///                 anyway, the transaction is rolled back 'completely' to the 'starting' point.
        ///                     An error message is added for the administrative user telling them to
        ///                         try again as deletion has completely failed. The method then
        ///                             returns a url for the user details page.
        ///                         
        ///     13. In case the clients successfully deleted the user from their ends, the
        ///         transaction is committed. A success message is added for the administrative
        ///             user, and the method returns the RootRoute.
        /// </summary>
        /// <param name="inputModel">Input model containing the id of the user to delete</param>
        /// <returns>A url of the RootRoute or user details page, depending on scenario</returns>
        public async Task<string> ManageDelete(DeleteUserInputModel inputModel)
        {
            if (!ActionContext.ModelState.IsValid)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found.";

                return RootRoute;
            }

            ApplicationUser user = await UserManager.FindByIdAsync(inputModel.Id);

            if (user == null)
            {
                TempData[TempDataKeys.ErrorMessage] = "User not found";

                return RootRoute;
            }
            else
            {
                string userDetailsRoute = UrlHelper.Action("Details", "Users", new { Area = "Administration", Id = inputModel.Id });

                bool isUserProductOwner = await UserManager.IsInRoleAsync(user, AuthorizedRoles.ProductOwner);

                if (isUserProductOwner)
                {
                    TempData[TempDataKeys.ErrorMessage] = $"Cannot delete a user with role {AuthorizedRoles.ProductOwner}.";

                    return userDetailsRoute;
                }

                using IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync();

                bool userAccessToArchiveRecorded = await RecordUserAccess(user, UserAccessPurposes.Archive);

                if (!userAccessToArchiveRecorded)
                {
                    await transaction.RollbackAsync();

                    TempData[TempDataKeys.ErrorMessage] = "Could not delete user. Please try again.";

                    return userDetailsRoute;
                } else {
                    user.Archive();

                    // passing the method with a changed user object saves this change
                    // when updating the security stamp of the user
                    //
                    // more information:
                    // https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/UserManager.cs#L844
                    IdentityResult updateUserSecurityStamp = await UserManager.UpdateSecurityStampAsync(user);

                    if (!updateUserSecurityStamp.Succeeded)
                    {
                        await transaction.RollbackAsync();

                        // updating user security stamp failed, logging errors
                        foreach (IdentityError error in updateUserSecurityStamp.Errors)
                            Console.WriteLine(error.Description);

                        TempData[TempDataKeys.ErrorMessage] = "Could not delete user. Please try again.";

                        return userDetailsRoute;
                    }

                    string userArchivedSavePoint = "userArchived";

                    await transaction.CreateSavepointAsync(userArchivedSavePoint);

                    bool userAccessToDeleteRecorded = await RecordUserAccess(user, UserAccessPurposes.Delete);

                    if (!userAccessToDeleteRecorded)
                    {
                        await transaction.RollbackAsync();

                        TempData[TempDataKeys.ErrorMessage] = "Could not delete user. Please try again.";

                        return userDetailsRoute;
                    }
                    else
                    {
                        IdentityResult deleteUser = await UserManager.DeleteAsync(user);

                        if (!deleteUser.Succeeded)
                        {
                            await transaction.RollbackAsync();

                            // deleting user failed, logging errors
                            foreach (IdentityError error in deleteUser.Errors)
                                Console.WriteLine(error.Description);

                            TempData[TempDataKeys.ErrorMessage] = "Could not delete user. Please try again.";

                            return userDetailsRoute;
                        }
                        else
                        {
                            // send notifications to all clients of CIS to delete the user
                            IdentityResult deleteUserFromAllClients = await BackChannelNotificationService.SendBackChannelDeleteNotificationsForUserAsync(user);

                            if (!deleteUserFromAllClients.Succeeded)
                            {
                                if (deleteUserFromAllClients.Errors.Any(error => error.Description == InternalCustomErrors.UserPartiallyDeleted))
                                {
                                    await transaction.RollbackToSavepointAsync(userArchivedSavePoint);

                                    await transaction.CommitAsync();

                                    // send notifications to all clients of CIS to signout the user
                                    await BackChannelNotificationService.SendBackChannelLogoutNotificationsForUserAsync(user);

                                    TempData[TempDataKeys.ErrorMessage] = "Could not delete user properly. User was partially deleted and was archived to restrict access. Please try again.";

                                    return userDetailsRoute;
                                }
                                else
                                {
                                    await transaction.RollbackAsync();

                                    TempData[TempDataKeys.ErrorMessage] = "Could not delete user. Please try again.";

                                    return userDetailsRoute;
                                }
                            }
                            else
                            {
                                await transaction.CommitAsync();

                                TempData[TempDataKeys.SuccessMessage] = "User deleted.";

                                return RootRoute;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        ///     private async Task<bool> RecordUserAccess(
        ///         ApplicationUser user, string purpose
        ///     )
        ///     
        ///     Records the action of accessing a user's data.
        ///     
        ///     1. Creates a new UserAccessRecord object which holds the
        ///         accessor's id, the user's (user whose data is being accessed) id,
        ///             and the purpose of the access.
        ///             
        ///     2. Persists this object in the data base using a try-catch block,
        ///         returning true when successful or returning false when a possible
        ///             exception happens or throwing an excepting when an unexpected
        ///                 exception occurs.
        /// </summary>
        /// <param name="user">The user whose data is being accessed</param>
        /// <param name="purpose">The purpose of accessing this data</param>
        /// <returns>Boolean indicating the result</returns>
        private async Task<bool> RecordUserAccess(ApplicationUser user, string purpose)
        {
            ApplicationUser accessor = await UserManager.GetUserAsync(ActionContext.HttpContext.User);

            UserAccessRecord userAccessRecord = new UserAccessRecord()
            {
                UserId = user.Id,
                AccessorId = accessor.Id,
                Purpose = purpose
            };

            try
            {
                await DbContext.UserAccessRecords.AddAsync(userAccessRecord);

                await DbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception exception)
            {
                if (exception is DbUpdateException || exception is DbUpdateConcurrencyException)
                {
                    return false;
                }

                throw;
            }
        }


        /// <summary>
        ///     private Expression<Func<ApplicationUser, bool>> GenerateNamesFilter(
        ///         string firstName, string lastName, bool firstNameEmpty, bool lastNameEmpty
        ///     )
        ///     
        ///     Creates a search filter to query the database with the first and last
        ///         names of the user.
        ///         
        ///     1. If the first and last name are both present in the method params, the
        ///         filter is created to query with both params.
        ///         
        ///     2. If there is only the first or the last name, then the filter is created
        ///         to query by that name only.
        ///         
        ///     3. Finally, the method returns the created filter. In case neither the first
        ///         nor the last name is available in the method params, the method returns null.
        /// </summary>
        /// <param name="firstName">First name search parameter</param>
        /// <param name="lastName">First name search parameter</param>
        /// <param name="firstNameEmpty">Boolean indicating if the firstname parameter is empty</param>
        /// <param name="lastNameEmpty">Boolean indicating if the lastname parameter is empty</param>
        /// <returns>The created search filter or null</returns>
        private Expression<Func<ApplicationUser, bool>> GenerateNamesFilter(
            string firstName, string lastName, bool firstNameEmpty, bool lastNameEmpty
        ) {
            Expression<Func<ApplicationUser, bool>> filter = null;

            string firstNameLowerCase = firstName?.ToLower();
            string lastNameLowerCase = lastName?.ToLower();

            if (!firstNameEmpty && !lastNameEmpty)
            {
                filter = user => user.FirstName.ToLower().Contains(firstNameLowerCase) && user.LastName.ToLower().Contains(lastNameLowerCase);
            }
            else if (!firstNameEmpty)
            {
                filter = user => user.FirstName.ToLower().Contains(firstNameLowerCase);
            }
            else if (!lastNameEmpty)
            {
                filter = user => user.LastName.ToLower().Contains(lastNameLowerCase);
            }

            return filter;
        }


        public void Dispose()
        {
            if (ResourcesDisposed)
                return;

            UserManager.Dispose();
            DbContext.Dispose();
            ResourcesDisposed = true;
        }
    }
}
