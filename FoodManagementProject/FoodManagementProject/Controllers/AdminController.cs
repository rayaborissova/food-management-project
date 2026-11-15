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

        public AdminController(FoodManagementDbContext context, HouseholdService households)
        {
            _context = context;
            _households = households;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.AppUsers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLowerInvariant();
                query = query.Where(u => u.Email.Contains(search) || u.DisplayName.ToLower().Contains(search));
            }

            return View(new AdminUserViewListModel
            {
                Search = search,
                CurrentUserId = CurrentUserId,
                Users = await query.OrderBy(u => u.Email).ToListAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string role)
        {
            if (id == CurrentUserId)
            {
                TempData["AdminError"] = "You can't change your own role";
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

        public async Task<IActionResult> SetActive(int id, bool active) 
        { 
            if (id == CurrentUserId)
            {
                TempData["AdminError"] = "You can't deactivae your own account.";
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
    }
}
