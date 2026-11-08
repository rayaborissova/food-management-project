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
        private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
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

            var personalInventoryCount = household.PersonalUserId != null ? await _context.InventoryBatches.CountAsync(b => b.HouseholdId == HouseholdId) : 0;

            return new HouseholdIndexViewModel
            {
                HouseholdName = household.HouseholdName,
                IsShared = isShared,
                Members = members,
                ActiveCode = invite?.Code,
                CodeExpiresAt = invite?.ExpiresAt,
                PersonalInventoryCount = personalInventoryCount,
                CurrentUserId = UserId,
                IsOwner = isShared && members.Any(m => m.UserId  == UserId && m.HouseholdRole == "owner") // checking if the user is the owner
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
                return RedirectToAction(nameof(Index));
            }

            var personalHouseholdId = HouseholdId;

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

            var copiedCount = 0;

            if (model.CopyInventory)
            {
                var personalBatches = await _context.InventoryBatches.AsNoTracking().Where(b => b.HouseholdId == personalHouseholdId).ToListAsync();

                var copies = personalBatches.Select(b => new InventoryBatch
                {
                    IngredientId = b.IngredientId,
                    HouseholdId = household.HouseholdId,
                    UnitId = b.UnitId,
                    Quantity = b.Quantity,
                    ExpirationDate = b.ExpirationDate,
                    DateAdded = DateTime.UtcNow
                }).ToList();

                _context.InventoryBatches.AddRange(copies);
                await _context.SaveChangesAsync();
                copiedCount = copies.Count();
            }

            await transaction.CommitAsync();

            TempData["HouseholdMessage"] = copiedCount > 0 ? $"Household created with {copiedCount} ingredient batches copied from your personal inventory." : "Household created"; //success messages
            return RedirectToAction(nameof(Index));
        }

        // when leaving a shared household, the user returns to the personal one
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(bool confirmDelete)
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

            //when last member is leaving, application needs an explicit confirmation
            if (successor == null && !confirmDelete)
            {
                TempData["HouseholdError"] = "You are the last member, so leaving would delete this household. Try again and confirm the warning.";
            }

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
        public async Task<IActionResult> RemoveMember(int memberUserId)
        {
            // only the owner can remove members
            var isOwner = await _context.HouseholdMembers.AnyAsync(m => m.UserId == UserId && m.HouseholdId == HouseholdId && m.HouseholdRole == "owner");

            if (!isOwner || !await InSharedHouseholdAsync())
            {
                TempData["HouseholdError"] = "Only the owner of a shared household can remove members.";
                return RedirectToAction(nameof(Index));
            }

            // owner cannot remove themselves the same way they would remove a member, they have to use the leave button
            if (memberUserId == UserId)
            {
                TempData["HouseholdError"] = "Use \"Leave household\" to remove yourself from the household.";
                return RedirectToAction(nameof(Index));
            }

            // the member they wish to remove should be part of the household
            var member = await _context.HouseholdMembers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == memberUserId && m.HouseholdId == HouseholdId);

            if (member == null) 
            {
                TempData["HouseholdError"] = "This person is not a member of this household.";
                return RedirectToAction(nameof(Index));
            }

            if (member.HouseholdRole == "owner")
            {
                TempData["HouseholdError"] = "The owner can't be removed.";
                return RedirectToAction(nameof(Index));
            }

            // after removal, the user is returned to their personal inventory
            var personalHouseholdId = await _context.Households.Where(h => h.PersonalUserId == memberUserId).Select(h => h.HouseholdId).FirstOrDefaultAsync();

            if (personalHouseholdId == 0)
            {
                TempData["HouseholdError"] = "The member's personal household could not be found.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // transfering the user to their personal inventory
            member.HouseholdId = personalHouseholdId;
            member.HouseholdRole = "owner";
            member.DateJoined = Now;
            await _context.SaveChangesAsync();

            // deleting the current invite code if one exists
            await _context.HouseholdInvites.Where(i => i.HouseholdId == HouseholdId).ExecuteDeleteAsync();
            await transaction.CommitAsync();

            TempData["HouseholdMessage"] = $"{member.User.DisplayName} was removed from the household. Create a new invite code if one is needed.";
            return RedirectToAction(nameof(Index));
        }

        // generating the invite code
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

        // joining a household using the invite code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join([Bind(Prefix = "Join")] JoinHouseholdViewModel model)
        {
            // allowing only one household per user, member of a shared one should leave their current one to join a new one
            if (await InSharedHouseholdAsync())
            {
                TempData["HouseholdError"] = "Leave your current household before joining another one.";
                return RedirectToAction(nameof(Index));
            }

            var code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
            var now = Now;

            var invite = await _context.HouseholdInvites.FirstOrDefaultAsync(i => i.Code == code && i.ExpiresAt > now);

            // error message for unknown or expired codes
            if (!ModelState.IsValid || invite == null)
            {
                TempData["HouseholdError"] = "Invalid or expired invite code.";
                return RedirectToAction(nameof(Index));
            }

            // transfering membership to the shared household
            var membership = await _context.HouseholdMembers.FirstAsync(m => m.UserId == UserId);
            membership.HouseholdId = invite.HouseholdId;
            membership.HouseholdRole = "member";
            membership.DateJoined = now;
            await _context.SaveChangesAsync();

            TempData["HouseholdMessage"] = "You joined the household. You now see its shared inventory and shopping list";
            return RedirectToAction(nameof(Index));
        }

    }
}
