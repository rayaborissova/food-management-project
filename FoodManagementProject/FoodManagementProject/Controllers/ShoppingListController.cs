
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodManagementProject.Models;

public class ShoppingListController : Controller
{
    private readonly FoodManagementDbContext _context;

    private const int TestHouseholdId = 1; // TO BE CHANGED
    public ShoppingListController(FoodManagementDbContext context)
    {
        _context = context;
    }

    // GET: SHOPPINGLISTITEMS
    [HttpGet]
    public async Task<IActionResult> Index()    
    {
        var items = await _context.ShoppingListItems.Where(item => item.HouseholdId == TestHouseholdId && !item.IsPurchased).
            Include(item => item.Ingredient).ThenInclude(ingredient => ingredient.Category). // to make shopping more effective
            Include(item=>item.Unit).OrderBy(item => item.Ingredient.Category.CategoryName).ThenBy(item => item.Ingredient.IngredientName).ToListAsync();


        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Add(string? search)
    {
        // load category names as well
        var ingredients = _context.Ingredients.Include(ingredient => ingredient.Category).AsQueryable();

        if(!string.IsNullOrWhiteSpace(search))
        {
            search= search.Trim().ToLowerInvariant();
            // same fuzzy search as the inventory search
            ingredients = ingredients.Where(ingredient => ingredient.IngredientNameNormalized.Contains(search) || EF.Functions.TrigramsSimilarity(ingredient.IngredientNameNormalized, search)>=0.17).OrderByDescending(ingredient => EF.Functions.TrigramsSimilarity(ingredient.IngredientNameNormalized,search)).ThenBy(ingredient => ingredient.IngredientName);
        } else
        {
            ingredients = ingredients.OrderBy(ingredient => ingredient.IngredientName);
        }

        ViewData["Search"] = search;

        return View(await ingredients.ToListAsync());

    }

    // GET: SHOPPINGLISTITEMS/Details/5
    public async Task<IActionResult> Details(int? shoppinglistitemid)
    {
        if (shoppinglistitemid == null)
        {
            return NotFound();
        }

        var shoppinglistitem = await _context.ShoppingListItems
            .FirstOrDefaultAsync(m => m.ShoppingListItemId == shoppinglistitemid);
        if (shoppinglistitem == null)
        {
            return NotFound();
        }

        return View(shoppinglistitem);
    }

    // GET: SHOPPINGLISTITEMS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SHOPPINGLISTITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ShoppingListItemId,HouseholdId,IngredientId,UnitId,Quantity,IsPurchased,ItemSource,DateAdded,Ingredient,Household,Unit")] ShoppingListItem shoppinglistitem)
    {
        if (ModelState.IsValid)
        {
            _context.Add(shoppinglistitem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(shoppinglistitem);
    }

    // GET: SHOPPINGLISTITEMS/Edit/5
    public async Task<IActionResult> Edit(int? shoppinglistitemid)
    {
        if (shoppinglistitemid == null)
        {
            return NotFound();
        }

        var shoppinglistitem = await _context.ShoppingListItems.FindAsync(shoppinglistitemid);
        if (shoppinglistitem == null)
        {
            return NotFound();
        }
        return View(shoppinglistitem);
    }

    // POST: SHOPPINGLISTITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? shoppinglistitemid, [Bind("ShoppingListItemId,HouseholdId,IngredientId,UnitId,Quantity,IsPurchased,ItemSource,DateAdded,Ingredient,Household,Unit")] ShoppingListItem shoppinglistitem)
    {
        if (shoppinglistitemid != shoppinglistitem.ShoppingListItemId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(shoppinglistitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShoppingListItemExists(shoppinglistitem.ShoppingListItemId))
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
        return View(shoppinglistitem);
    }

    // GET: SHOPPINGLISTITEMS/Delete/5
    public async Task<IActionResult> Delete(int? shoppinglistitemid)
    {
        if (shoppinglistitemid == null)
        {
            return NotFound();
        }

        var shoppinglistitem = await _context.ShoppingListItems
            .FirstOrDefaultAsync(m => m.ShoppingListItemId == shoppinglistitemid);
        if (shoppinglistitem == null)
        {
            return NotFound();
        }

        return View(shoppinglistitem);
    }

    // POST: SHOPPINGLISTITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? shoppinglistitemid)
    {
        var shoppinglistitem = await _context.ShoppingListItems.FindAsync(shoppinglistitemid);
        if (shoppinglistitem != null)
        {
            _context.ShoppingListItems.Remove(shoppinglistitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ShoppingListItemExists(int? shoppinglistitemid)
    {
        return _context.ShoppingListItems.Any(e => e.ShoppingListItemId == shoppinglistitemid);
    }
}
