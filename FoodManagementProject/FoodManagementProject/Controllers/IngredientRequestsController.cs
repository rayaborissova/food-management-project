using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FoodManagementProject.Models;
using FoodManagementProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Controllers
{
    [Authorize(Roles = AppRoles.Regular)]
    public class IngredientRequestsController : Controller
    {
        private const int MaxPendingPerUser = 10;
        private readonly FoodManagementDbContext _context;

        public IngredientRequestsController(FoodManagementDbContext context)
        {
            _context = context;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<IngredientRequestPageViewModel> BuildPageAsync(IngredientRequestFormViewModel form)
        {
            var userId = CurrentUserId;

            return new IngredientRequestPageViewModel
            {
                Form = form,
                Requests = await _context.IngredientRequests.AsNoTracking().Include(r => r.SuggestedCategory).Where(r => r.RequestedBy == userId).OrderByDescending(r => r.DateCreated).ToListAsync(),
                Categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync()
            };
        }

        //name can be prefilled from the add pages' search
        [HttpGet]
        public async Task<IActionResult> Index(string? name)
        {
            var form = new IngredientRequestFormViewModel { Name = name?.Trim() ?? string.Empty };
            return View(await BuildPageAsync(form));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind(Prefix = "Form")] IngredientRequestFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", await BuildPageAsync(form));
            }

            var userId = CurrentUserId;
            var cleanName = IngredientNormalizer.Clean(form.Name);
            var normalized = IngredientNormalizer.Normalize(form.Name);

            if (await _context.Ingredients.AnyAsync(i => i.IngredientNameNormalized == normalized)) 
            {
                ModelState.AddModelError("Form.Name", "This ingredient is already in the catalogue. Search for it in the Add page.");
            } else if (await _context.IngredientRequests.AnyAsync( r=> r.RequestStatus == RequestStatuses.Pending && r.RequestedName.ToLower() == normalized)) {
                ModelState.AddModelError("Form.Name", "This ingredient has already been requested and is waiting for a review.");
            }

            if (form.SuggestedCategoryId != null && await _context.Categories.AnyAsync(c => c.CategoryId == form.SuggestedCategoryId))
            {
                ModelState.AddModelError("Form.SuggestedCategoryId", "Choose a valid category.");
            }

            var pendingCount = await _context.IngredientRequests.CountAsync(r => r.RequestedBy == userId && r.RequestStatus == RequestStatuses.Pending);
            if (pendingCount >= MaxPendingPerUser)
            {
                ModelState.AddModelError("", $"You can have at most {MaxPendingPerUser} requests waiting for review.");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", await BuildPageAsync(form));
            }

            _context.IngredientRequests.Add(new IngredientRequest
            {
                RequestedBy = userId,
                RequestedName = cleanName,
                SuggestedCategoryId = form.SuggestedCategoryId,
                Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim(),
                RequestStatus = RequestStatuses.Pending,
                DateCreated = Now
            });

            await _context.SaveChangesAsync();

            TempData["RequestMessage"] = "Your request was submitted for review.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var userId = CurrentUserId;

            var removed = await _context.IngredientRequests.Where(r => r.RequestId == id && r.RequestStatus == RequestStatuses.Pending).ExecuteDeleteAsync();

            TempData["RequestMessage"] = removed > 0 ? "Request withdrawn." : "That request can no longer be withdrawn.";
            return RedirectToAction(nameof(Index));
        
        }
    }
}
