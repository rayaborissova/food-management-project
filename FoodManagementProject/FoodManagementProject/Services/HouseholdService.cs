using FoodManagementProject.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Services
{
    public class HouseholdService
    {
        private readonly FoodManagementDbContext _context;

        public HouseholdService(FoodManagementDbContext context)
        {
            _context = context;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        public async Task<bool> LeaveSharedHouseholdAsync(int userId)
        {
            var membership = await _context.HouseholdMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (membership == null)
            {
                return false;
            }

            var leftHouseholdId = membership.HouseholdId;
            var isShared = await _context.Households.AnyAsync(h => h.HouseholdId == leftHouseholdId && h.PersonalUserId == null);
            if (!isShared)
            {
                return false;
            }

            var wasOwner = membership.HouseholdRole == "owner";

            var personalHouseholdId = await _context.Households.Where(h => h.PersonalUserId == userId).Select(h => h.HouseholdId).FirstAsync();

            var successor = await _context.HouseholdMembers.Where(m => m.HouseholdId == leftHouseholdId && m.UserId != userId).OrderBy(m => m.DateJoined).FirstOrDefaultAsync();

            membership.HouseholdId = personalHouseholdId;
            membership.HouseholdRole = "owner";
            membership.DateJoined = Now;

            if (successor != null && wasOwner)
            {
                successor.HouseholdRole = "owner";
            }

            await _context.SaveChangesAsync();

            if (successor == null)
            {
                await _context.Households.Where(h => h.HouseholdId == leftHouseholdId).ExecuteDeleteAsync();
            }

            return true;

        }
    }
}
