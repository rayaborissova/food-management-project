using Microsoft.AspNetCore.Mvc;
using FoodManagementProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Html;

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
                "suggestions" => await _context.IngredientRequests.CountAsync(r => r.RequestStatus == RequestStatuses.Pending),
                "open-reports" => await _context.IngredientReports.CountAsync(r => r.ReportStatus == ReportStatuses.Open),
                "reports" => await _context.IngredientReports.CountAsync(r => r.ReportStatus == ReportStatuses.Forwarded),
                _ => 0
            };

            return new HtmlContentViewComponentResult (new HtmlString(count > 0 ? $"<span class=\"nav-badge\">{count}</span>" : string.Empty));
        }
    }
}
