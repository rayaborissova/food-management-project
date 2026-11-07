using FoodManagementProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace FoodManagementProject.Controllers
{
    [Authorize]
    public class HouseholdController : HouseholdControllerBase
    {
        private readonly FoodManagementDbContext _context;
        private const string CodeAlphabet = "ABCDEFGHIJKMNOPQRSTUVWXYZ0123456789";
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

            var isShared = household.PersonalUserId == null;
            var now = Now;

            var invite = isShared ? await _context.HouseholdInvites.Where(i => i.HouseholdId == HouseholdId && i.ExpiresAt > now).OrderByDescending(i => i.DateCreated).FirstOrDefaultAsync() : null;

            return new HouseholdIndexViewModel
            {
                HouseholdName = household.HouseholdName,
                IsShared = isShared,
                Members = members,
                ActiveCode = invite?.Code,
                CodeExpiresAt = invite?.ExpiresAt
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

        // when leaving a shared household, the user returns to the personal one
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave()
        {
            //the user can't leave their personal household
            if (!await InSharedHouseholdAsync()) 
            {
                TempData["HouseholdError"] = "You are already using your personal household.";
                return RedirectToAction(nameof(Index));
            }

            var membership = await _context.HouseholdMembers.FirstAsync(m => m.UserId == UserId);
            var leftHouseholdId = membership.HouseholdId;
            var wasOwner = membership.HouseholdRole == "owner";

            var personalHouseholdId = await _context.Households.Where(h => h.PersonalUserId == UserId).Select(h => h.HouseholdId).FirstAsync();

            //locating the "oldest" member, if there is one
            var successor = await _context.HouseholdMembers.Where(m => m.HouseholdId == leftHouseholdId && m.UserId != UserId).OrderBy(m => m.DateJoined).FirstOrDefaultAsync();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            //returning to personal household
            membership.HouseholdId = personalHouseholdId;
            membership.HouseholdRole = "owner";
            membership.DateJoined = Now;

            //ownership passes to the "oldest" member when the owner leaves
            if (successor != null && wasOwner)
            {
                successor.HouseholdRole = "owner";
            }

            await _context.SaveChangesAsync();

            //if last member left, household gets deleted
            //ADD POPUP WARNING BEFORE DELETING
            if (successor == null)
            {
                await _context.Households.Where(h => h.HouseholdId == leftHouseholdId).ExecuteDeleteAsync();
            }

            await transaction.CommitAsync();

            TempData["HouseholdMessage"] = "You left the household and returned to your personal inventory.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateInvite()
        {
            if (!await InSharedHouseholdAsync()) 
            {
                TempData["HouseholdError"] = "Create or join a shared household first.";
                return RedirectToAction(nameof(Index));
            }

            await _context.HouseholdInvites.Where(i => i.HouseholdId == HouseholdId).ExecuteDeleteAsync();

            _context.HouseholdInvites.Add(new HouseholdInvite
            {
                HouseholdId = HouseholdId,
                CreatedBy = UserId,
                Code = RandomNumberGenerator.GetString(CodeAlphabet, 8), // code is randomly generated using the previously set characters
                DateCreated = Now,
                ExpiresAt = Now.AddDays(7)
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }

    }
}
