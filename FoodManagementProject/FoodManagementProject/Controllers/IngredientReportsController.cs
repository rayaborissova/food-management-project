using Microsoft.AspNetCore.Mvc;
using FoodManagementProject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace FoodManagementProject.Controllers
{
    [Authorize(Roles = AppRoles.Regular)]
    public class IngredientReportsController : Controller
    {
        private const int MaxActivePerUser = 20;

        private readonly FoodManagementDbContext _context;

        public IngredientReportsController(FoodManagementDbContext context)
        {
            _context = context;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;

            var reports = await _context.IngredientReports.AsNoTracking().Include(r => r.Ingredient).Where(r => r.ReportedBy == userId).OrderByDescending(r => r.DateCreated).ToListAsync();

            return View(reports);
        }

        private async Task<ReportFormViewModel?> BuildFormAsync(int ingredientId, ReportFormViewModel? posted = null)
        {
            var ingredient = await _context.Ingredients.AsNoTracking().Include(i => i.Category).FirstOrDefaultAsync(i => i.IngredientId == ingredientId);

            if (ingredient == null)
            {
                return null;
            }

            return new ReportFormViewModel
            {
                IngredientId = ingredientId,
                Reason = posted?.Reason ?? string.Empty,
                SuggestedCategoryId = posted?.SuggestedCategoryId,
                Note = posted?.Note,
                IngredientName = ingredient.IngredientName,
                CurrentCategoryName = ingredient.Category?.CategoryName ?? "None",
                Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync()
            };
        }

        [HttpGet]
        public async Task<IActionResult> Create(int ingredientId)
        {
            var userId = CurrentUserId;

            if (await _context.IngredientReports.AnyAsync(r => r.IngredientId == ingredientId && r.ReportedBy == userId && (r.ReportStatus == ReportStatuses.Open || r.ReportStatus == ReportStatuses.Forwarded)))
            {
                TempData["ReportMessage"] = "You already have an active report on this ingredient.";
                return RedirectToAction(nameof(Index));
            }

            var model = await BuildFormAsync(ingredientId);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportFormViewModel form)
        {
            var userId = CurrentUserId;
            var note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim();

            if (!ReportReasons.All.Any(r => r.Value == form.Reason))
            {
                ModelState.AddModelError(nameof(form.Reason), "Choose a reason.");
            }

            // if a note is needed so the moderator knows what to check, wrong category can use the suggested category instead
            if (form.Reason != ReportReasons.WrongCategory && note == null)
            {
                ModelState.AddModelError(nameof(form.Note), "Add a short note so the moderator knows what to check.");
            }

            if (form.SuggestedCategoryId != null && !await _context.Categories.AnyAsync(c => c.CategoryId == form.SuggestedCategoryId))
            {
                ModelState.AddModelError(nameof(form.SuggestedCategoryId), "Choose a valid category.");
            }

            if (!await _context.Ingredients.AnyAsync(i => i.IngredientId == form.IngredientId))
            {
                return NotFound();
            }

            if (!await _context.IngredientReports.AnyAsync(r => r.IngredientId == form.IngredientId && r.ReportedBy == userId && (r.ReportStatus == ReportStatuses.Open || r.ReportStatus == ReportStatuses.Forwarded)))
            {
                ModelState.AddModelError("", "You already have an active report on this ingredient.");
            }

            var activeCount = await _context.IngredientReports.CountAsync(r => r.ReportedBy == userId && (r.ReportStatus == ReportStatuses.Open || r.ReportStatus == ReportStatuses.Forwarded));

            if (activeCount >= MaxActivePerUser)
            {
                ModelState.AddModelError("", $"You can have at most {MaxActivePerUser} active reports.");
            }

            if (!ModelState.IsValid)
            {
                var invalid = await BuildFormAsync(form.IngredientId, form);
                return View(invalid);
            }

            _context.IngredientReports.Add(new IngredientReport
            {
                IngredientId = form.IngredientId, 
                ReportedBy = userId, 
                Reason = form.Reason,
                Note = form.Note,
                SuggestedCategoryId = form.Reason == ReportReasons.WrongCategory ? form.SuggestedCategoryId : null, //only makes sense for a category report
                ReportStatus = ReportStatuses.Open,
                DateCreated = Now
            });

            await _context.SaveChangesAsync();

            TempData["ReportMessage"] = "Thank you. Your report was sent for review.";
            return RedirectToAction(nameof(Index));
        }

        // withdrawing a report that has not been handled yet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = CurrentUserId;

            var removed = await _context.IngredientReports.Where(r => r.ReportId == id && r.ReportedBy == userId && r.ReportStatus == ReportStatuses.Open).ExecuteDeleteAsync();

            TempData["ReportMessage"] = removed > 0 ? "Report withdrawn." : "That report can no longer be withdrawn.";
            return RedirectToAction(nameof(Index));
        }




    }
}
