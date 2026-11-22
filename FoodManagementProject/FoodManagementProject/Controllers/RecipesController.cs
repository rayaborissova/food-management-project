using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FoodManagementProject.Models;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.VisualBasic;
using System.Security.Cryptography.X509Certificates;

namespace FoodManagementProject.Controllers
{
    [Authorize(Roles = AppRoles.Regular)]
    public class RecipesController : HouseholdControllerBase
    {
        private readonly FoodManagementDbContext _context;

        public RecipesController(FoodManagementDbContext context)
        {
            _context = context;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        // the ingredients this household has in its inventory rigth now
        private async Task<List<int>> OwnedIngredientIdsAsync()
        {
            return await _context.InventoryBatches.AsNoTracking().Where(b => b.HouseholdId == HouseholdId).Select(b => b.IngredientId).Distinct().ToListAsync();
        }

        // my recipes
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = UserId;
            var owned = await OwnedIngredientIdsAsync();

            var recipes = await _context.Recipes.AsNoTracking().Where(r => r.AuthorId == userId).OrderByDescending(r => r.DateUpdated).Select(r => new RecipeCardViewModel
            {
                RecipeId = r.RecipeId,
                Title = r.Title,
                AuthorName = r.Author.DisplayName,
                Visibility = r.Visibility,
                Servings = r.Servings,
                TotalMinutes = (r.PrepMinutes ?? 0) + (r.CookMinutes ?? 0),
                IngredientCount = r.Ingredients.Count,
                HaveCount = r.Ingredients.Count(i => owned.Contains(i.IngredientId)),
                DateUpdated = r.DateUpdated
            }).ToListAsync();

            return View(recipes);
        }

        // details - author's own recipes and every public one
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = UserId;

            var recipe = await _context.Recipes.AsNoTracking().Include(r => r.Author).Include(r => r.Ingredients).ThenInclude(i => i.Ingredient).Include(r => r.Ingredients).ThenInclude(r => r.Unit)
                .FirstOrDefaultAsync(r => r.RecipeId == id && (r.AuthorId == userId || r.Visibility == RecipeVisibilities.Public));

            if (recipe == null)
            {
                return NotFound();
            }

            return View(new RecipeDetailsViewModel
            {
                Recipe = recipe,
                IsAuthor = recipe.AuthorId == userId,
                OwnedIngredientIds = (await OwnedIngredientIdsAsync()).ToHashSet()
            });
        }

        // ingredient search for the form (JSON)
        [HttpGet]
        public async Task<IActionResult> SearchIngredients(string? q)
        {
            q = (q ?? string.Empty).Trim().ToLowerInvariant();

            if (q.Length < 2)
            {
                return Json(Array.Empty<object>());
            }

            var items = await _context.Ingredients.AsNoTracking().Where(i => i.IngredientNameNormalized.Contains(q) || EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized, q) >= 0.17)
                .OrderByDescending(i => EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized, q)).Take(15).Select(i => new { id = i.IngredientId, name = i.IngredientName, category = i.Category.CategoryName })
                .ToListAsync();

            return Json(items);
        }

        // units for the dropdowns and the ingredient names of the lines, always read from database
        private async Task FillDisplayAsync(RecipeFormViewModels form)
        {
            form.Units = await _context.Units.AsNoTracking().OrderBy(u => u.UnitName).ToListAsync();

            var ids = form.Lines.Select(l => l.IngredientId).Distinct().ToList();

            var names = await _context.Ingredients.AsNoTracking().Where(i => ids.Contains(i.IngredientId)).ToDictionaryAsync(i => i.IngredientId, i => i.IngredientName);

            foreach (var line in form.Lines)
            {
                line.IngredientName = names.TryGetValue(line.IngredientId, out var name) ? name : "Unknown ingredient";
            }
        }

        private async Task ValidateFormAsync(RecipeFormViewModels form)
        {
            if (form.Visibility != RecipeVisibilities.Private && form.Visibility != RecipeVisibilities.Public)
            {
                ModelState.AddModelError(nameof(form.Visibility), "Choose who can see this recipe.");
            }

            if (form.Lines.Count == 0)
            {
                ModelState.AddModelError("", "Add at least one ingredient.");
            }

            var ids = form.Lines.Select(l => l.IngredientId).Distinct().ToList();

            if (ids.Count != form.Lines.Count)
            {
                ModelState.AddModelError("", "An ingredient appears more than once. Combine the amounts in one line.");
            }

            var names = await _context.Ingredients.AsNoTracking().Where(i => ids.Contains(i.IngredientId)).ToDictionaryAsync(i => i.IngredientId, i => i.IngredientName);

            var unitIds = await _context.Units.AsNoTracking().Select(u => u.UnitId).ToListAsync();

            foreach (var line in form.Lines)
            {
                if (!names.TryGetValue(line.IngredientId, out var name))
                {
                    ModelState.AddModelError("", "A selected ingredient no longer exists.");
                    continue;
                }

                if (line.Quantity.HasValue != line.UnitId.HasValue)
                {
                    ModelState.AddModelError("", $"For {name}, enter both quantity and unit, or leave both empty.");
                }

                if (line.Quantity.HasValue)
                {
                    if (line.Quantity.Value <= 0)
                    {
                        ModelState.AddModelError("", $"The quantity of {name} must be gerater than zero.");
                    }
                    else if (line.Quantity.Value > 99999999.99m || decimal.Round(line.Quantity.Value, 2) != line.Quantity.Value)
                    {
                        ModelState.AddModelError("", $"The quantity of {name} may have at most two decimal places and be no greater than 99,999,999.99.");
                    }
                }

                if (line.UnitId.HasValue && !unitIds.Contains(line.UnitId.Value))
                {
                    ModelState.AddModelError("", $"Choose a valid unit for {name}.");
                }

                if (line.Note != null && line.Note.Length > 100)
                {
                    ModelState.AddModelError("", $"The note for {name} may be at most 100 characters.");
                }
            }
        }

        private static List<RecipeIngredient> ToEntities(IEnumerable<RecipeIngredientLineViewModel> lines)
        {
            return lines.Select(l => new RecipeIngredient
            {
                IngredientId = l.IngredientId,
                UnitId = l.UnitId,
                Quantity = l.Quantity,
                Note = string.IsNullOrWhiteSpace(l.Note) ? null : l.Note.Trim()
            }).ToList();
        }

        // create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var form = new RecipeFormViewModels();
            await FillDisplayAsync(form);
            return View("Form", form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeFormViewModels form)
        {
            await ValidateFormAsync(form);

            if (!ModelState.IsValid)
            {
                await FillDisplayAsync(form);
                return View("Form", form);
            }

            var recipe = new Recipe
            {
                AuthorId = UserId,
                Title = form.Title.Trim(),
                Servings = form.Servings,
                PrepMinutes = form.PrepMinutes,
                CookMinutes = form.CookMinutes,
                Instructions = form.Instructions.Trim(),
                Visibility = form.Visibility,
                DateCreated = Now,
                DateUpdated = Now,
                Ingredients = ToEntities(form.Lines)
            };

            // the recipe and its lines are saved together in one transaction
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            TempData["RecipeMessage"] = "Recipe saved.";
            return RedirectToAction(nameof(Details), new { id = recipe.RecipeId });
        }

        // edit for the author only
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = UserId;

            var recipe = await _context.Recipes.AsNoTracking().Include(r => r.Ingredients).FirstOrDefaultAsync(r => r.RecipeId == id && r.AuthorId == userId);

            if (recipe == null)
            {
                return NotFound();
            }

            var form = new RecipeFormViewModels
            {
                RecipeId = recipe.RecipeId,
                Title = recipe.Title,
                Servings = recipe.Servings,
                PrepMinutes = recipe.PrepMinutes,
                CookMinutes = recipe.CookMinutes,
                Instructions = recipe.Instructions,
                Visibility = recipe.Visibility,
                Lines = recipe.Ingredients.Select(i => new RecipeIngredientLineViewModel
                {
                    IngredientId = i.IngredientId,
                    Quantity = i.Quantity,
                    UnitId = i.UnitId,
                    Note = i.Note
                }).ToList()
            };

            await FillDisplayAsync(form);
            return View("Form", form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecipeFormViewModels form)
        {
            var userId = UserId;

            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.RecipeId == id && r.AuthorId == userId);

            if (recipe == null)
            {
                return NotFound();
            }

            form.RecipeId = id;
            await ValidateFormAsync(form);

            if (!ModelState.IsValid)
            {
                await FillDisplayAsync(form);
                return View("Form", form);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // lines are replaced as a whole, deleting to avoid clashes with the unique rule for recipe and ingredient
            await _context.RecipeIngredients.Where(i => i.RecipeId == id).ExecuteDeleteAsync();

            recipe.Title = form.Title.Trim();
            recipe.Servings = form.Servings;
            recipe.PrepMinutes = form.PrepMinutes;
            recipe.CookMinutes = form.CookMinutes;
            recipe.Instructions = form.Instructions.Trim();
            recipe.Visibility = form.Visibility;
            recipe.DateUpdated = Now;

            var lines = ToEntities(form.Lines);
            foreach (var line in lines)
            {
                line.RecipeId = id;
            }

            _context.RecipeIngredients.AddRange(lines);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["RecipeMessage"] = "Recipe updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // delete, only available for the author
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = UserId;

            var recipe = await _context.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.RecipeId == id && r.AuthorId == userId);

            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = UserId;

            // the lines are removed by the cascade
            var removed = await _context.Recipes.Where(r => r.RecipeId == id && r.AuthorId == userId).ExecuteDeleteAsync();
            if (removed == 0)
            {
                return NotFound();
            }

            TempData["RecipeMessage"] = "Recipe deleted.";
            return RedirectToAction(nameof(Index));
        }

    }
}
