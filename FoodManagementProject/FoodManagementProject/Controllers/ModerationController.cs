using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodManagementProject.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using FoodManagementProject.Services;

namespace FoodManagementProject.Controllers
{
    [Authorize(Roles = AppRoles.Moderator)]
    public class ModerationController : Controller
    {
        private const string SourceLabel = "user request";

        private readonly FoodManagementDbContext _context;
        public ModerationController(FoodManagementDbContext context)
        {
            _context = context;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        //pending suggestions, oldest first
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pending = await _context.IngredientRequests.AsNoTracking().Include(r => r.Requester).Include(r => r.SuggestedCategory).Where(r => r.RequestStatus == RequestStatuses.Pending).OrderBy(r => r.DateCreated).ToListAsync();
            ViewData["OpenReports"] = await _context.IngredientReports.CountAsync(r => r.ReportStatus == ReportStatuses.Open);
            return View(pending);
        }

        //viewing most recent decisions
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var reviewed = await _context.IngredientRequests.AsNoTracking().Include(r => r.Requester).Include(r => r.Reviewer).Where(r => r.RequestStatus != RequestStatuses.Pending).OrderByDescending(r => r.DateReviewed).Take(100).ToListAsync();

            return View(reviewed);
        }

        private async Task<ModerationReviewViewModel?> LoadReviewAsync(int id, string? finalName = null, int? categoryId = null, string? reviewNote = null)
        {
            var request = await _context.IngredientRequests.AsNoTracking().Include(r => r.Requester).Include(r => r.SuggestedCategory).FirstOrDefaultAsync(r => r.RequestId == id && r.RequestStatus == RequestStatuses.Pending);

            if (request == null)
            {
                return null;
            }

            var normalized = IngredientNormalizer.Normalize(request.RequestedName);

            return new ModerationReviewViewModel
            {
                RequestId = id,
                Request = request,
                FinalName = finalName ?? request.RequestedName,
                CategoryId = categoryId ?? request.SuggestedCategoryId,
                ReviewNote = reviewNote,
                Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(),
                Similar = await _context.Ingredients.AsNoTracking().Include(i => i.Category).Where(i => EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized, normalized) >= 0.3).OrderByDescending(i => EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized, normalized)).Take(5).ToListAsync()
            };

        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var model = await LoadReviewAsync(id);

            if (model == null)
            {
                TempData["ModerationError"] = "This suggestion was not found or has already been reviewed.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(ModerationReviewViewModel form)
        {
            var finalName = IngredientNormalizer.Clean(form.FinalName ?? string.Empty);
            var normalized = IngredientNormalizer.Normalize(finalName);


            if (finalName.Length == 0 || finalName.Length > 150)
            {
                ModelState.AddModelError(nameof(form.FinalName), "Enter an ingredient name (up to 150 characters).");
            }
            else if (await _context.Ingredients.AnyAsync(i => i.IngredientNameNormalized == normalized))
            {
                ModelState.AddModelError(nameof(form.FinalName), "An ingredient with this name already exists. Reject the suggestion as a duplicate instead.");
            }

            if (form.CategoryId == null || !await _context.Categories.AnyAsync(c => c.CategoryId == form.CategoryId))
            {
                ModelState.AddModelError(nameof(form.CategoryId), "Choose a category.");
            }

            if (!ModelState.IsValid)
            {
                var invalid = await LoadReviewAsync(form.RequestId, form.FinalName, form.CategoryId, form.ReviewNote);
                if (invalid == null)
                {
                    TempData["ModerationError"] = "This suggestion was not found or has already been reviewed.";
                    return RedirectToAction(nameof(Index));
                }
                return View("Review", invalid);
            }

            var note = string.IsNullOrWhiteSpace(form.ReviewNote) ? null : form.ReviewNote.Trim();
            var reviewerId = CurrentUserId;
            var now = Now;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var claimed = await _context.IngredientRequests.Where(r => r.RequestId == form.RequestId && r.RequestStatus == RequestStatuses.Pending).ExecuteUpdateAsync(s => s.SetProperty(r => r.RequestStatus, RequestStatuses.Approved).SetProperty(r => r.ReviewedBy, (int?)reviewerId).SetProperty(r => r.DateReviewed, (DateTime?)now).SetProperty(r => r.ReviewNote, note));

            if (claimed == 0)
            {
                await transaction.RollbackAsync();
                TempData["ModerationError"] = "This suggestion was already reviewed by someone else.";
                return RedirectToAction(nameof(Index));
            }

            var ingredient = new Ingredient
            {
                CategoryId = form.CategoryId!.Value,
                IngredientName = finalName,
                IngredientNameNormalized = normalized,
                IngredientSource = SourceLabel
            };
            _context.Ingredients.Add(ingredient);

            await _context.SaveChangesAsync();

            await _context.IngredientRequests.Where(r => r.RequestId == form.RequestId).ExecuteUpdateAsync(s => s.SetProperty(r => r.IngredientId, (int?)ingredient.IngredientId));

            await transaction.CommitAsync();

            TempData["ModerationMessage"] = $"\"{finalName}\" was added to the catalogue.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(ModerationReviewViewModel form)
        {
            var note = (form.ReviewNote ?? string.Empty).Trim();

            if (note.Length == 0 || note.Length > 300)
            {
                ModelState.AddModelError(nameof(form.ReviewNote), "Give a short reason (up to 300 characters). The user will see it.");

                var invalid = await LoadReviewAsync(form.RequestId, form.FinalName, form.CategoryId, form.ReviewNote);
                if (invalid == null)
                {
                    TempData["ModerationError"] = "This suggestion was not found or has already been reviewed.";
                    return RedirectToAction(nameof(Index));
                }

                return View("Review", invalid);
            }

            var reviewerId = CurrentUserId;
            var now = Now;

            var claimed = await _context.IngredientRequests.Where(r => r.RequestId == form.RequestId && r.RequestStatus == RequestStatuses.Pending).ExecuteUpdateAsync(s => s.SetProperty(r => r.RequestStatus, RequestStatuses.Rejected).SetProperty(r => r.ReviewedBy, (int?)reviewerId).SetProperty(r => r.DateReviewed, (DateTime?)now).SetProperty(r => r.ReviewNote, note));

            TempData[claimed > 0 ? "ModerationMessage" : "ModerationError"] = claimed > 0 ? "Suggestion rejected." : "This suggestion was already reviewed by someone else.";

            return RedirectToAction(nameof(Index));

        }

        // opening reports, oldest first
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var open = await _context.IngredientReports.AsNoTracking().Include(r => r.Ingredient).ThenInclude(i => i.Category).Include(r => r.Reporter).Where(r => r.ReportStatus == ReportStatuses.Open).OrderBy(r => r.DateCreated).ToListAsync();

            return View(open);
        }

        [HttpGet]
        public async Task<IActionResult> ReviewReport(int id)
        {
            var model = await IngredientReportLoader.LoadDecisionAsync(_context, id, ReportStatuses.Open);

            if (model == null)
            {
                TempData["ModerationError"] = "This report was not found or has already been handled.";
                return RedirectToAction(nameof(Reports));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> DismissReport(ReportDecisionViewModel form)
        {
            return DecideReportAsync(form, ReportStatuses.Dismissed, "Explain briefly why nothing needs to change. The reporter will see it.", "Report dismissed.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ForwardReport(ReportDecisionViewModel form)
        {
            return DecideReportAsync(form, ReportStatuses.Forwarded, "Tell the admin what is wrong and what the fix should be.", "Report forwarded to the admin.");
        }

        private async Task<IActionResult> DecideReportAsync(ReportDecisionViewModel form, string newStatus, string noteError, string successMessage)
        {
            var note = (form.Note ?? string.Empty).Trim();

            if (note.Length == 0 || note.Length > 300)
            {
                ModelState.AddModelError(nameof(form.Note), noteError);

                var invalid = await IngredientReportLoader.LoadDecisionAsync(_context, form.ReportId, ReportStatuses.Open, form.Note);

                if (invalid == null)
                {
                    TempData["ModerationError"] = "This report was not found or has already been handled.";
                    return RedirectToAction(nameof(Reports));
                }

                return View("ReviewReport", invalid);
            }

            var reviewerId = CurrentUserId;
            var now = Now;

            // the update only matches while the report is still open, so two moderators can't both decide it
            var claimed = await _context.IngredientReports.
                Where(r => r.ReportId == form.ReportId && r.ReportStatus == ReportStatuses.Open).
                ExecuteUpdateAsync(s => s.
                SetProperty(r => r.ReportStatus, newStatus).
                SetProperty(r => r.ReviewedBy, (int?)reviewerId).
                SetProperty(r => r.DateReviewed, (DateTime?)now).
                SetProperty(r => r.ReviewNote, note));

            TempData[claimed > 0 ? "ModerationMessage" : "ModerationError"] = claimed > 0 ? successMessage : "This report was already handled by someone else.";

            return RedirectToAction(nameof(Reports));
        }

        [HttpGet]
        public async Task<IActionResult> ReportHistory()
        {
            var handled = await _context.IngredientReports.AsNoTracking().Include(r => r.Ingredient).Include(r => r.Reporter).Include(r => r.Reviewer).Where(r => r.ReportStatus != ReportStatuses.Open).OrderByDescending(r => r.DateReviewed).Take(100).ToListAsync();

            return View(handled);
        }
    }





}
