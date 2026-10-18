
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodManagementProject.Models;
using Humanizer;
using Microsoft.DotNet.Scaffolding.Shared.Project;

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
            Include(item => item.Unit).OrderBy(item => item.Ingredient.Category.CategoryName).ThenBy(item => item.Ingredient.IngredientName).ToListAsync();


        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Add(string? search)
    {
        // load category names as well
        var ingredients = _context.Ingredients.Include(ingredient => ingredient.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLowerInvariant();
            // same fuzzy search as the inventory search
            ingredients = ingredients.Where(ingredient => ingredient.IngredientNameNormalized.Contains(search) || EF.Functions.TrigramsSimilarity(ingredient.IngredientNameNormalized, search) >= 0.17).OrderByDescending(ingredient => EF.Functions.TrigramsSimilarity(ingredient.IngredientNameNormalized, search)).ThenBy(ingredient => ingredient.IngredientName);
        }
        else
        {
            ingredients = ingredients.OrderBy(ingredient => ingredient.IngredientName);
        }

        ViewData["Search"] = search;

        return View(await ingredients.ToListAsync());

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int[] selectedIngredientIds)
    {
        if (selectedIngredientIds == null || selectedIngredientIds.Length == 0)
        {
            return RedirectToAction(nameof(Add));
        }

        //removing repeated ids
        var ingredientIds = selectedIngredientIds.Distinct().ToArray();

        //retrieving ingredients from database
        var ingredients = await _context.Ingredients.Where(ingredient => ingredientIds.Contains(ingredient.IngredientId)).OrderBy(ingredient => ingredient.IngredientName).ToListAsync();

        if (ingredients.Count != ingredientIds.Length)
        {
            return BadRequest("One or more selected ingredients could not be found");
        }

        var model = new ShoppingListReviewViewModel
        {
            Items = ingredients.Select(ingredient => new ShoppingListReviewItemViewModel
            {
                IngredientId = ingredient.IngredientId,
                IngredientName = ingredient.IngredientName
            }).ToList(),
            AvailableUnits = await _context.Units.OrderBy(unit => unit.UnitName).ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ShoppingListReviewViewModel model)
    {
        if (model.Items.Count == 0)
        {
            return RedirectToAction(nameof(Add));
        }

        var ingredientIds = model.Items.Select(item => item.IngredientId).Distinct().ToArray();

        var ingredients = await _context.Ingredients.Where(ingredient => ingredientIds.Contains(ingredient.IngredientId)).ToDictionaryAsync(ingredient => ingredient.IngredientId);

        model.AvailableUnits = await _context.Units.OrderBy(unit => unit.UnitName).ToListAsync();

        if (ingredientIds.Length != model.Items.Count)
        {
            ModelState.AddModelError("", "An ingredient was submitted more than once");
        }

        for (int i = 0; i < model.Items.Count; i++)
        {
            var item = model.Items[i];
            if (!ingredients.TryGetValue(item.IngredientId, out var ingredient))
            {
                ModelState.AddModelError("", "A selected ingredient no longer exists");
                continue;
            }

            item.IngredientName = ingredient.IngredientName;

            if (item.Quantity.HasValue)
            {
                if (item.Quantity.Value <= 0)
                {
                    ModelState.AddModelError($"Items[{i}].Quantity", "Quantity must be greater than zero");
                }

                if (item.Quantity.Value > 99999999.99m || decimal.Round(item.Quantity.Value, 2) != item.Quantity.Value)
                {
                    ModelState.AddModelError($"Items[{i}].Quantity", "Use at most two decimal places and a quantity no greater than 99999999.99");
                }
            }

            if (item.Quantity.HasValue != item.UnitId.HasValue)
            {
                ModelState.AddModelError($"Items[{i}].Quantity", "Enter both quantity and unit, or leave both empty");
            }
            if (item.UnitId.HasValue)
            {
                var unit = model.AvailableUnits.FirstOrDefault(unit => unit.UnitId == item.UnitId.Value);

                if (unit == null)
                {
                    ModelState.AddModelError($"Items[{i}].UnitId", "Choose a valid unit.");
                }
                else if (unit.Abbreviation == "pc" && item.Quantity.HasValue && item.Quantity.Value % 0.25m != 0)
                { //TO BE CHANGED
                    ModelState.AddModelError($"Items[{i}].Quantity", "Use increments of 0.25 pieces");
                }
            }

        }

        if (!ModelState.IsValid)
        {
            return View("Review", model);
        }

        if (!await _context.Households.AnyAsync(household => household.HouseholdId == TestHouseholdId)) // TO BE CHANGED
        {
            ModelState.AddModelError("", "The test household can't be found");
            return View("Review", model);
        }

        var existingIngredientIds = await _context.ShoppingListItems.Where(item => item.HouseholdId == TestHouseholdId && !item.IsPurchased && ingredientIds.Contains(item.IngredientId)).Select(item => item.IngredientId).ToListAsync(); // TO BE CHANGED

        var dateAdded = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var newItems = model.Items.Where(item => !existingIngredientIds.Contains(item.IngredientId)).Select(item => new ShoppingListItem
        {
            HouseholdId = TestHouseholdId, // TO BE CHANGED
            IngredientId = item.IngredientId,
            Quantity = item.Quantity,
            UnitId = item.UnitId,
            IsPurchased = false,
            ItemSource = "manual",
            DateAdded = dateAdded
        }).ToList();

        _context.ShoppingListItems.AddRange(newItems);
        await _context.SaveChangesAsync();

        var skippedCount = model.Items.Count - newItems.Count;

        TempData["ShoppingListSuccess"] = $"{newItems.Count} items added. " + $"{skippedCount} already on the shopping list were skipped.";

        return RedirectToAction(nameof(Index));
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

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.ShoppingListItems.Include(item => item.Ingredient).
            FirstOrDefaultAsync(item => item.ShoppingListItemId == id.Value && item.HouseholdId == TestHouseholdId && !item.IsPurchased); // TO BE CHANGED

        if (item == null)
        {
            return NotFound();
        }

        var model = new ShoppingListEditViewModel
        {
            ShoppingListItemId = item.ShoppingListItemId,
            IngredientName = item.Ingredient.IngredientName,
            Quantity = item.Quantity,
            UnitId = item.UnitId,
            AvailableUnits = await _context.Units.OrderBy(unit => unit.UnitName).ToListAsync()
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, ShoppingListEditViewModel model)
    {
        if (id == null || id.Value != model.ShoppingListItemId)
        {
            return NotFound();
        }

        var item = await _context.ShoppingListItems.Include(entry => entry.Ingredient).FirstOrDefaultAsync(entry => entry.ShoppingListItemId == id.Value && entry.HouseholdId == TestHouseholdId && !entry.IsPurchased); // TO BE CHANGED

        if (item == null)
        {
            return NotFound();
        }

        model.IngredientName = item.Ingredient.IngredientName;
        model.AvailableUnits = await _context.Units.OrderBy(unit => unit.UnitName).ToListAsync();

        if (model.Quantity.HasValue)
        {
            if (model.Quantity.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.Quantity), "Quantity must be greater than zero");
            }

            if (model.Quantity.Value > 99999999.99m || decimal.Round(model.Quantity.Value, 2) != model.Quantity.Value)
            {
                ModelState.AddModelError(nameof(model.Quantity), "Use at most two decimal places and a quantity no greater than 99999999.99");
            }
        }

        if (model.Quantity.HasValue != model.UnitId.HasValue)
        {
            ModelState.AddModelError(nameof(model.Quantity), "Enter both quantity and unit, or leave both empty.");
        }

        if (model.UnitId.HasValue)
        {
            var unit = model.AvailableUnits.FirstOrDefault(unit => unit.UnitId == model.UnitId.Value);

            if (unit == null)
            {
                ModelState.AddModelError(nameof(model.UnitId), "Choose a valid unit.");
            }
            else if (unit.Abbreviation == "pc" && model.Quantity.HasValue && model.Quantity.Value % 0.25m != 0) //TO BE CHANGED
            {
                ModelState.AddModelError(nameof(model.Quantity), "Use increments of 0.25 pieces");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        item.Quantity = model.Quantity;
        item.UnitId = model.UnitId;

        await _context.SaveChangesAsync();

        TempData["ShoppingListEditSuccess"] = "Shopping list item updated";

        return RedirectToAction(nameof(Index));
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
