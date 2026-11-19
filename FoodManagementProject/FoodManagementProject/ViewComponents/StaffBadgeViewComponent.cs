using Microsoft.AspNetCore.Mvc;
using FoodManagementProject.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.ViewComponents
{
    public class StaffBadgeViewComponent : ViewComponent
    {
        private readonly FoodManagementDbContext _context;

        public StaffBadgeViewComponent(FoodManagementDbContext context)
        {
            _context = context;
        }

        // the number of items waiting fo a staff role are shown next to its menu link
        public async Task<IViewComponentResult> InvokeAsync(string kind)
        {
            var count = kind switch
            {
                "moderation" => await _context.IngredientRequests.CountAsync(r => r.RequestStatus == RequestStatuses.Pending) + await _context.IngredientReports.CountAsync(r => r.ReportStatus == ReportStatuses.Open),
                "reports" => await _context.IngredientReports.CountAsync(r => r.ReportStatus == ReportStatuses.Forwarded),
                _ => 0
            };

            return Content(count > 0 ? $"{count}" : string.Empty);
        }
    }
}
