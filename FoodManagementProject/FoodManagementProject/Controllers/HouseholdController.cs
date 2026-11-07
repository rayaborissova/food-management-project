using FoodManagementProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodManagementProject.Controllers
{
    [Authorize]
    public class HouseholdController : HouseholdControllerBase
    {
        private readonly FoodManagementDbContext _context;
        public HouseholdController(FoodManagementDbContext context)
        {
            _context = context;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        //true when the user is in a shared household rather than a personal one
        private Task<bool> InSharedHouseholdAsync()
        {
            return _context.Households.AnyAsync(h => h.HouseholdId == HouseholdId && h.PersonalUserId == null);
        }

        private async Task<HouseholdIndexViewModel> BuildModelAsync()
        {
            var household = await _context.Households.FirstAsync(h => h.HouseholdId == HouseholdId);
            var members = await _context.HouseholdMembers.Where(m=> m.HouseholdId == HouseholdId).Include(m => m.User).OrderBy(m => m.DateJoined).ToListAsync();

            return new HouseholdIndexViewModel
            {
                HouseholdName = household.HouseholdName,
                IsShared = household.PersonalUserId == null,
                Members = members
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await BuildModelAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "Create")] CreateHouseholdViewModel model ) 
        { 
            // errors when creating a new household
            if (await InSharedHouseholdAsync())
            {
                TempData["HouseholdError"] = "Leave your current household before creating a new one"; 
                return RedirectToAction(nameof(Index));
            }
            
            if (!ModelState.IsValid)
            {
                TempData["HouseholdError"] = "Enter a household name (up to 100 characters)";
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            //creating the new household
            var household = new Household
            {
                HouseholdName = model.Name.Trim(), 
                DateCreated = Now, 
                PersonalUserId = null //marks a shared household
            };
            _context.Households.Add(household);
            await _context.SaveChangesAsync();

            //moving the membership row to the new household
            var membership = await _context.HouseholdMembers.FirstAsync(m => m.UserId == UserId);
            membership.HouseholdId = household.HouseholdId;
            membership.HouseholdRole = "owner"; //setting role to owner
            membership.DateJoined = Now;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["HousheoldMessage"] = "Household created."; //success message
            return RedirectToAction(nameof(Index));
        }

    }
}
