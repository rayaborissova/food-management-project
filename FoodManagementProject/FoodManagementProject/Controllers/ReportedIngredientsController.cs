using Microsoft.AspNetCore.Mvc;
using FoodManagementProject.Models;
using FoodManagementProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace FoodManagementProject.Controllers
{
    [Authorize(Roles =AppRoles.Admin)]
    public class ReportedIngredientsController : Controller
    {
        private readonly FoodManagementDbContext _context;

        public ReportedIngredientsController(FoodManagementDbContext context)
        {
            _context = context;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // reports that moderators confirmed, oldest first
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(new ReportedIngredientsViewModel
            {
                Forwarded = await _context.IngredientReports.AsNoTracking().Include(r => r.Ingredient).ThenInclude(i => i.Category).Include(r => r.Reporter).Include(r => r.Reviewer).Where(r => r.ReportStatus == ReportStatuses.Forwarded).OrderBy(r => r.DateReviewed).ToListAsync(),
                Resolved = await _context.IngredientReports.AsNoTracking().Include(r => r.Ingredient).Include(r => r.Resolver).Where(r => r.ReportStatus == ReportStatuses.Resolved).OrderBy(r => r.DateResolved).Take(30).ToListAsync()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Resolve(int id)
        {
            var model = await IngredientReportLoader.LoadDecisionAsync(_context, id, ReportStatuses.Forwarded);

            if (model == null)
            {
                TempData["ReportAdminError"] = "This report was not found or has already been closed.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(ReportDecisionViewModel form)
        {
            var note = (form.Note ?? string.Empty).Trim();
            
            if(form.Resolution != ReportResolutions.Fixed && form.Resolution != ReportResolutions.NoChange)
            {
                ModelState.AddModelError(nameof(form.Resolution), "Choose an outcome.");
            }

            if (note.Length == 0 || note.Length > 300)
            {
                ModelState.AddModelError(nameof(form.Note), "Descibe what you did, or why nothing changed. The reporter will see it.");
            }

            if(!ModelState.IsValid)
            {
                var invalid = await IngredientReportLoader.LoadDecisionAsync(_context, form.ReportId, ReportStatuses.Forwarded, form.Note, form.Resolution);
                if (invalid == null)
                {
                    TempData["ReportAdminError"] = "This report was not found or has already been closed.";
                    return RedirectToAction(nameof(Index));
                }
                return View(invalid);
            }

            var adminId = CurrentUserId;
            var now = Now;
            var resolution = form.Resolution;

            //update only matches while the report is still forwarded
            var claimed = await _context.IngredientReports.Where(r => r.ReportId == form.ReportId && r.ReportStatus == ReportStatuses.Forwarded).ExecuteUpdateAsync(s => s.SetProperty(r => r.ReportStatus, ReportStatuses.Resolved).SetProperty(r => r.ResolvedBy, (int?)adminId).SetProperty(r => r.DateResolved, (DateTime?)now).SetProperty(r => r.Resolution, resolution).SetProperty(r => r.ResolutionNote, note));

            TempData[claimed > 0 ? "ReportAdminMessage" : "ReportAdminError"] = claimed > 0 ? "Report closed." : "This report was already closed";

            return RedirectToAction(nameof(Index));

        } 


    }
}
