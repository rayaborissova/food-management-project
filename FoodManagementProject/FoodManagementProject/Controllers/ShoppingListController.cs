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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPurchase(int[] purchasedItemIds)
    {
        if (purchasedItemIds == null || purchasedItemIds.Length == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var itemIds = purchasedItemIds.Distinct().ToArray();

        var items = await _context.ShoppingListItems.Include(item => item.Ingredient).Include(item => item.Unit).Where(item => itemIds.
        Contains(item.ShoppingListItemId) && item.HouseholdId == TestHouseholdId && !item.IsPurchased).OrderBy(item => item.Ingredient.IngredientName).ToListAsync(); // TO BE CHANGED

        if (items.Count != itemIds.Length)
        {
            return BadRequest("One or more selected shopping list entries are no longer available");
        }

        var model = new ShoppingListPurchaseViewModel
        {
            Items = items.Select(item => new ShoppingListPurchaseItemViewModel
            {
                ShoppingListItemId = item.ShoppingListItemId,
                IngredientName = item.Ingredient.IngredientName,
                Quantity = item.Quantity,
                UnitId = item.UnitId,
                UnitName = item.Unit?.UnitName ?? "Not specified"
            }).ToList()
        };

        return View(model);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAsListed(int[] purchasedItemIds)
    {
        if (purchasedItemIds == null || purchasedItemIds.Length == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var itemIds = purchasedItemIds.Distinct().ToArray();

        var items = await _context.ShoppingListItems.Where(item => itemIds
        .Contains(item.ShoppingListItemId) && item.HouseholdId == TestHouseholdId && !item.IsPurchased).ToListAsync(); //TO BE CHANGED

        if (items.Count != itemIds.Length)
        {
            return BadRequest("One or more selected entries are no longer available. Return to the shopping list and refresh.");
        }

        var dateAdded = DateTime.UtcNow;

        foreach (var item in items)
        {
            var batch = new InventoryBatch
            {
                IngredientId = item.IngredientId,
                HouseholdId = item.HouseholdId,
                Quantity = item.Quantity,
                UnitId = item.UnitId,
                ExpirationDate = null,
                DateAdded = dateAdded
            };

            _context.InventoryBatches.Add(batch);

            //marking as completed, no longer to be displaye
            item.IsPurchased = true;
        }

        await _context.SaveChangesAsync();

        TempData["InventoryPurchaseSuccess"] = $"{items.Count} purchased items added to inventory.";

        return RedirectToAction("Index", "Inventory");
    }

    // opening the editable details for the selected purchases

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPurchase(int[] purchasedItemIds)
    {
        if (purchasedItemIds == null || purchasedItemIds.Length == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var itemIds = purchasedItemIds.Distinct().ToArray();

        var items = await _context.ShoppingListItems.Include(item => item.Ingredient).Include(item => item.Unit).
            Where(item => itemIds.Contains(item.ShoppingListItemId) && item.HouseholdId == TestHouseholdId && !item.IsPurchased).OrderBy(item => item.Ingredient.IngredientName).ToListAsync(); // TO BE CHANGED

        if (items.Count != itemIds.Length)
        {
            return BadRequest("One or more shopping list entries are no longer available.");
        }

        var model = new ShoppingListPurchaseViewModel
        {
            Items = items.Select(item => new ShoppingListPurchaseItemViewModel
            {
                ShoppingListItemId = item.ShoppingListItemId,
                IngredientName = item.Ingredient.IngredientName,
                Quantity = item.Quantity,
                UnitId = item.UnitId,
                UnitName = item.Unit?.UnitName ?? "Not specified"
            }).ToList(),
            AvailableUnits = await _context.Units.OrderBy(unit => unit.UnitName).ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePurchase(ShoppingListPurchaseViewModel model)
    {
        if (model.Items.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var itemIds = model.Items.Select(row => row.ShoppingListItemId).Distinct().ToArray();

        if (itemIds.Length != model.Items.Count)
        {
            ModelState.AddModelError("", "A shopping list entry was submitted more than once.");
        }

        var entries = await _context.ShoppingListItems.Include(e => e.Ingredient).Where(e => itemIds.
        Contains(e.ShoppingListItemId) && e.HouseholdId == TestHouseholdId && !e.IsPurchased).ToDictionaryAsync(e => e.ShoppingListItemId); // TO BE CHANGED

        if (entries.Count != itemIds.Length)
        {
            return BadRequest("One or more selected entries are no longer available. Return to the shopping list and refresh.");
        }

        model.AvailableUnits = await _context.Units.OrderBy(unit => unit.UnitName).ToListAsync();

        for (int i = 0; i < model.Items.Count; i++)
        {
            var row = model.Items[i];
            var entry = entries[row.ShoppingListItemId];

            row.IngredientName = entry.Ingredient.IngredientName;

            if (row.Quantity.HasValue)
            {
                if (row.Quantity.Value <= 0)
                {
                    ModelState.AddModelError($"Items[{i}].Quantity", "Quantity must be greater than zero.");
                }
                if (row.Quantity.Value > 99999999.99m || decimal.Round(row.Quantity.Value, 2) != row.Quantity.Value)
                {
                    ModelState.AddModelError($"Items[{i}].Quantity", "Use at most two decimal places and a quantity no greater than 99999999.99");
                }

            }
            if (row.Quantity.HasValue != row.UnitId.HasValue)
            {
                ModelState.AddModelError($"Items[{i}].Quantity", "Enter both quantity and unit, or leave both empty.");
            }

            if (row.UnitId.HasValue)
            {
                var unit = model.AvailableUnits.FirstOrDefault(unit => unit.UnitId == row.UnitId.Value);

                if (unit == null)
                {
                    ModelState.AddModelError($"Items[{i}].UnitId", "Choose a valid unit.");
                }
                else if (unit.Abbreviation == "pc" && row.Quantity.HasValue && row.Quantity.Value % 0.25m != 0) //TO BE CHANGED
                {
                    ModelState.AddModelError($"Items[{i}].Quantity", "Use increments of 0.25 pieces");
                }
            }

        }

        if (!ModelState.IsValid)
        {
            return View("EditPurchase", model);
        }

        var dateAdded = DateTime.UtcNow;

        foreach (var row in model.Items)
        {
            var entry = entries[row.ShoppingListItemId];
            var batch = new InventoryBatch
            {
                IngredientId = entry.IngredientId,
                HouseholdId = entry.HouseholdId,

                // using the ones entered from the form
                Quantity = row.Quantity,
                UnitId = row.UnitId,
                ExpirationDate = row.ExpirationDate,
                DateAdded = dateAdded
            };

            _context.InventoryBatches.Add(batch);

            entry.IsPurchased = true;
        }

        await _context.SaveChangesAsync();

        TempData["InventoryPurchaseSuccess"] = $"{model.Items.Count} purchased items added to inventory.";

        return RedirectToAction("Index", "Inventory");
    }

    // GET: SHOPPINGLISTITEMS/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var item = await _context.ShoppingListItems.Include(e => e.Ingredient).Include(e => e.Unit)
            .FirstOrDefaultAsync(e => e.ShoppingListItemId == id.Value && e.HouseholdId == TestHouseholdId && !e.IsPurchased); // TO BE CHANGED
        
        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    // POST: SHOPPINGLISTITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if(id==null)
        {
            return NotFound();
        }

        var item = await _context.ShoppingListItems.
            FirstOrDefaultAsync(e=>e.ShoppingListItemId == id.Value && e.HouseholdId == TestHouseholdId && !e.IsPurchased); // TO BE CHANGED
        
        if (item == null)
        {
            return NotFound();
        }

        _context.ShoppingListItems.Remove(item);

        await _context.SaveChangesAsync();

        TempData["ShoppingListDeleteSuccess"] = "Item removed from the shopping list.";

        return RedirectToAction(nameof(Index));
    }

}
