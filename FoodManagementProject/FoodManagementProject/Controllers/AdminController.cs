using System.Security.Claims;
using FoodManagementProject.Models;
using FoodManagementProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace FoodManagementProject.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {

        private readonly FoodManagementDbContext _context;
        private readonly HouseholdService _households;

        private static readonly string[] ValidRoles = { AppRoles.Regular, AppRoles.Moderator, AppRoles.Admin };

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        public AdminController(FoodManagementDbContext context, HouseholdService households)
        {
            _context = context;
            _households = households;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // the user list, searching and filtering are happening in the browser
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(new AdminUserViewListModel
            {
                CurrentUserId = CurrentUserId,
                Users = await _context.AppUsers.AsNoTracking().OrderBy(u => u.Email).ToListAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string role)
        {
            if (id == CurrentUserId)
            {
                TempData["AdminError"] = "You can't change your own role.";
                return RedirectToAction(nameof(Index));
            }

            if (!ValidRoles.Contains(role))
            {
                TempData["AdminError"] = "Unknown role";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // admins and moderators don't use fridge so they leave first
            if (role != AppRoles.Regular)
            {
                await _households.LeaveSharedHouseholdAsync(id);
            }

            user.UserRole = role;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["AdminMessage"] = $"{user.DisplayName} is now {role}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id, bool active)
        {
            if (id == CurrentUserId)
            {
                TempData["AdminError"] = "You can't deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.AppUsers.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // deactivated account shouldn't be part of a shared household or own one
            if (!active)
            {
                await _households.LeaveSharedHouseholdAsync(id);
            }

            user.IsActive = active;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["AdminMessage"] = active ? $"{user.DisplayName} was reactivated." : $"{user.DisplayName} was deactivated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (id == CurrentUserId)
            {
                TempData["AdminError"] = "You can't delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id == CurrentUserId)
            {
                TempData["AdminError"] = "You can't delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _context.AppUsers.AnyAsync(u => u.UserId == id))
            {
                return NotFound();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _households.LeaveSharedHouseholdAsync(id);
            await _context.AppUsers.Where(u => u.UserId == id).ExecuteDeleteAsync();

            await transaction.CommitAsync();

            TempData["AdminMessage"] = "The account was permanently deleted.";
            return RedirectToAction(nameof(Index));
        }

        // admin start page (numbers for the things only admin manages)
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var now = Now;

            var roles = await _context.AppUsers.AsNoTracking().GroupBy(u => u.UserRole).Select(g => new { Role = g.Key, Count = g.Count() }).ToListAsync();

            int RoleCount(string role) => roles.FirstOrDefault(r => r.Role == role)?.Count ?? 0;

            var deactivated = await _context.AppUsers.CountAsync(u => !u.IsActive);
            var sharedHouseholds = await _context.Households.CountAsync(h => h.PersonalUserId == null);
            var ingredients = await _context.Ingredients.CountAsync();
            var categories = await _context.Categories.CountAsync();

            // reports the moderators confirmed and forwarded
            var forwarded = await _context.IngredientReports.AsNoTracking().Where(r => r.ReportStatus == ReportStatuses.Forwarded).Select(r => r.DateReviewed ?? r.DateCreated).ToListAsync();

            var (waitingHint, waitingStatus) = DashboardHelper.WaitingHint(forwarded, now);

            return View(new DashboardViewModel
            {
                Heading = "Dashboard",
                Lead = "Users, the ingredient catalogue and the reports waiting for you.",
                Cards = new List<SummaryCardViewModel>
                {
                    new() {Title = "Users", Value= roles.Sum(r=>r.Count).ToString(),
                        Hint = $"{RoleCount(AppRoles.Regular)} regular · {RoleCount(AppRoles.Moderator)} moderators · {RoleCount(AppRoles.Admin)} admins",
                        Controller = "Admin", Action = "Index" },
                    new() {Title = "Deactivated accounts", Value= deactivated.ToString(),
                        Controller = "Admin", Action = "Index" },
                    new() {Title = "Reports waiting for you", Value= forwarded.Count.ToString(),
                        Hint = waitingHint, HintStatus = waitingStatus,
                        Controller = "ReportedIngredients", Action = "Index" },
                    new() {Title = "Ingredients", Value= ingredients.ToString(),
                        Hint = $"{categories} categories",
                        Controller = "Ingredients", Action = "Index" },
                    new() {Title = "Shared households", Value= sharedHouseholds.ToString() }
                }
            });


        }



    }
}
