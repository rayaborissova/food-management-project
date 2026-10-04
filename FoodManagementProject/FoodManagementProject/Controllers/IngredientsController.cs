
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodManagementProject.Models;

namespace FoodManagementProject.Controllers { 
    public class IngredientsController : Controller
    {
        private readonly FoodManagementDbContext _context;

        public IngredientsController(FoodManagementDbContext context)
        {
            _context = context;
        }

        // get ingredients
        public async Task<IActionResult> Index(string search)   
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IngredientId,CategoryId,IngredientName,IngredientNameNormalized,IngredientSource,Status")] Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ingredient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ingredient);
        }

        // GET: INGREDIENTS/Edit/5
        public async Task<IActionResult> Edit(int? id)
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
            return View(ingredient);
        }

        // POST: INGREDIENTS/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("IngredientId,CategoryId,IngredientName,IngredientNameNormalized,IngredientSource,Status")] Ingredient ingredient)
        {
            if (id != ingredient.IngredientId)
            {
                return NotFound();
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
                return RedirectToAction(nameof(Index));
            }
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

        // POST: INGREDIENTS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient != null)
            {
                _context.Ingredients.Remove(ingredient);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IngredientExists(int? ingredientid)
        {
            return _context.Ingredients.Any(e => e.IngredientId == ingredientid);
        }
    }
}