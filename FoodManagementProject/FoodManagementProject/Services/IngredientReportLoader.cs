using Microsoft.AspNetCore.Mvc;
using FoodManagementProject.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Services
{
    public class IngredientReportLoader 
    {
        //loading a report in the given status for a decision
        public static async Task<ReportDecisionViewModel?> LoadDecisionAsync(
            FoodManagementDbContext context, int id, string status, string? note = null, string? resolution = null)
        {
            var report = await context.IngredientReports.AsNoTracking().Include(r => r.Ingredient).ThenInclude(i => i.Category).Include(r => r.Reporter).Include(r => r.SuggestedCategory).Include(r => r.Reviewer).FirstOrDefaultAsync(r => r.ReportId == id && r.ReportStatus == status);

            if (report == null)
            {
                return null;
            }

            var others = await context.IngredientReports.CountAsync(r => r.IngredientId == report.IngredientId && r.ReportId != id && (r.ReportStatus == ReportStatuses.Open || r.ReportStatus == ReportStatuses.Forwarded));

            return new ReportDecisionViewModel
            {
                ReportId = id,
                Note = note,
                Resolution = resolution,
                Report = report,
                OtherActiveReports = others
            };
        }
    }
}
