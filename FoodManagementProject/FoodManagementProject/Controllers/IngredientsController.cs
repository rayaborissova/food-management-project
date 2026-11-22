
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodManagementProject.Models;
using Microsoft.AspNetCore.Authorization;
using FoodManagementProject.Services;

namespace FoodManagementProject.Controllers {

    [Authorize (Roles = AppRoles.Admin)]
    public class IngredientsController : Controller
    {
        private readonly FoodManagementDbContext _context;

        public IngredientsController(FoodManagementDbContext context)
        {
            _context = context;
        }

        // get ingredients
        public async Task<IActionResult> Index(string? search)   
        {
            var ingredients = _context.Ingredients.Include(i=>i.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)){ //if search is not null or a white space
                search = search.Trim().ToLower();
                //fuzzy search using the postgres extention pg_trgm
                ingredients = ingredients.Where(i => i.IngredientNameNormalized.Contains(search) || EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized, search) >= 0.17) // 0.17 was chosen based on the "chik" search, I wanted to output all chicken related ingredients
                    .OrderByDescending(i=> EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized,search)); 
                //either contains exactly or by string similarity; ordered by the biggest string % match
            }

            return View(await ingredients.ToListAsync());
        }

        // GET: INGREDIENTS/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(m => m.IngredientId == id);
            if (ingredient == null)
            {
                return NotFound();
            }

            return View(ingredient);
        }

        // GET: INGREDIENTS/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: INGREDIENTS/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IngredientId,CategoryId,IngredientName,IngredientNameNormalized,IngredientSource,Status")] Ingredient ingredient)
        {
            // search form is derived from the name, not typed by hand
            ingredient.IngredientNameNormalized = IngredientNormalizer.Normalize(ingredient.IngredientName ?? string.Empty);
            ModelState.Remove(nameof(Ingredient.IngredientNameNormalized));

            // no two ingredients have the same name
            if (await _context.Ingredients.AnyAsync(i => i.IngredientNameNormalized == ingredient.IngredientNameNormalized)) 
            {
                ModelState.AddModelError(nameof(Ingredient.IngredientName), "An ingredient with this name already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(ingredient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(ingredient);
        }

        // GET: INGREDIENTS/Edit/5
        public async Task<IActionResult> Edit(int? id, string? returnUrl)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }

            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null; // only addresses inside this site are kept
            return View(ingredient);
        }

        // POST: INGREDIENTS/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("IngredientId,CategoryId,IngredientName,IngredientNameNormalized,IngredientSource,Status")] Ingredient ingredient, string? returnUrl)
        {
            if (id != ingredient.IngredientId)
            {
                return NotFound();
            }

            // search form is derived from the name, not typed by hand
            ingredient.IngredientNameNormalized = IngredientNormalizer.Normalize(ingredient.IngredientName ?? string.Empty);
            ModelState.Remove(nameof(Ingredient.IngredientNameNormalized));

            // no two ingredients have the same name
            if (await _context.Ingredients.AnyAsync(i => i.IngredientNameNormalized == ingredient.IngredientNameNormalized && i.IngredientId != ingredient.IngredientId))
            {
                ModelState.AddModelError(nameof(Ingredient.IngredientName), "Another ingredient already has this name.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ingredient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IngredientExists(ingredient.IngredientId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    TempData["ReportAdminMessage"] = "Ingredient saved. You can now close the report below.";
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View(ingredient);
        }

        // GET: INGREDIENTS/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(m => m.IngredientId == id);
            if (ingredient == null)
            {
                return NotFound();
            }

            return View(ingredient);
        }

        // if an ingredient is in someone's inventory or shopping list
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient != null)
            {
                _context.Ingredients.Remove(ingredient);

                try
                {
                    await _context.SaveChangesAsync();
                } catch (DbUpdateException)
                {
                    TempData["IngredientError"] = "This ingredient is used in inventories or shopping lists and can't be deleted";
                    return RedirectToAction(nameof(Index));
                }


            }
            return RedirectToAction(nameof(Index));
        }

        private bool IngredientExists(int? ingredientid)
        {
            return _context.Ingredients.Any(e => e.IngredientId == ingredientid);
        }
    }
}