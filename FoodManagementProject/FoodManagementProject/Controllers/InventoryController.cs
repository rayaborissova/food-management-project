
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodManagementProject.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class InventoryController : Controller
{
    private readonly FoodManagementDbContext _context;


    private const int TestHouseholdId = 1; // creating a temporary household for testing

    public InventoryController(FoodManagementDbContext context)
    {
        _context = context;
    }

    // GET: INVENTORYBATCHS
    public async Task<IActionResult> Index()
    { 
        // retrieving the inventory batches, including the ingredient name, units; later quantity
        var inventory = await _context.InventoryBatches.Where(b => b.HouseholdId == TestHouseholdId).Include(b => b.Ingredient).Include(b => b.Unit)
            .OrderBy(b => b.Ingredient.IngredientName).ToListAsync();
        
        
        return View(inventory);
    }

    // this method will help load the ingredients into the searchable checklist
    public async Task<IActionResult> Add(string? search)
    {
        var ingredients = _context.Ingredients.Include(i => i.Category).AsQueryable(); // we want the category also to be visible

        if (!string.IsNullOrEmpty(search)) // if not search is not empty
        {
            search = search.Trim().ToLower(); // trim and put in lowercase to compare to the normalized names

            ingredients = ingredients.Where(i => i.IngredientNameNormalized.Contains(search) || EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized, search) >= 0.17).
                OrderByDescending(i => EF.Functions.TrigramsSimilarity(i.IngredientNameNormalized, search)); // either contains the search or is close to the search; threshold was chosen based on trial and error
                                                                                                             // the results are then sorted by their similarity score
        } else
        {
            ingredients = ingredients.OrderBy(i => i.IngredientName); // if search is empty, the ingredients are sorted alphabetically by name
        }

        ViewData["Search"] = search; // pass the search to the view under "Search"
        return View(await ingredients.ToListAsync()); // execute and display on the page
    }

    [HttpPost]
    [ValidateAntiForgeryToken] // to prevent possible malicious intentions
    public async Task<IActionResult> Review(int[] selectedIngredientIds)
    {
        // if no selected ingredients, returning to Add
        if (selectedIngredientIds.Length == 0)
        {
            return RedirectToAction(nameof(Add));
        }

        // receive the Ids and remove duplicates
        var selectedIds = selectedIngredientIds.Distinct().ToArray();

        // retrieving the corresponding ingredients from the database
        var ingredients = await _context.Ingredients.Where(i => selectedIds.Contains(i.IngredientId))
            .OrderBy(i => i.IngredientName).ToListAsync();

        // checks if all ingrediends are available, returns an error if something is missing
        if (ingredients.Count != selectedIds.Length)
        {
            return BadRequest("One or more ingredients no longer exist.");
        }

        
        var model = new InventoryReviewViewModel
        {
            // creating one review item per ingredient
            Items = ingredients.Select(i => new InventoryReviewItemViewModel { IngredientId = i.IngredientId, IngredientName = i.IngredientName }).ToList(),
            
            // loading the units from the dropdown
            AvailableUnits = await _context.Units.OrderBy(u => u.UnitName).ToListAsync()
        };

        return View(model);

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(InventoryReviewViewModel model)
    {
        //if no items are submitted, return to the add ingredients page
        if (model.Items.Count == 0)
        {
            return RedirectToAction(nameof(Add));
        }

        //extracting the IDs from each submitted item, without repetitions and putting them into an array
        var ingredientIds = model.Items.Select(item => item.IngredientId).Distinct().ToArray();

        //retrieving the ingredients by the IDs submitted 
        var ingredients = await _context.Ingredients.Where(i => ingredientIds.Contains(i.IngredientId)).ToDictionaryAsync(i => i.IngredientId);

        //reloading the units
        model.AvailableUnits = await _context.Units.OrderBy(u=>u.UnitName).ToListAsync();

        // comparing the number of unique IDs with the number of items
        if(ingredientIds.Length != model.Items.Count)
        {
            // if different, an ingridient was submitted more than once; a general form error is displayed
            ModelState.AddModelError("", "An ingredient was submitted more than once.");
        }

        for (int i = 0; i < model.Items.Count; i++)
        {
            var item = model.Items[i];

            // if the ID is not found, a general error is displayed
            if (!ingredients.TryGetValue(item.IngredientId, out var ingredient))
            {
                ModelState.AddModelError("", "A selected ingredient no longer exists.");
                continue; 
            }

            // using the name from the database
            item.IngredientName = ingredient.IngredientName;

            // if quantity is input and is below 0, an error is displayed
            if (item.Quantity.HasValue && item.Quantity.Value <= 0) {
                ModelState.AddModelError($"Items[{i}].Quantity", $"Quantity for {item.IngredientName} must be greater than zero.");
            }

            //if either quantity or unit are not present, an error is displayed
            if(item.Quantity.HasValue != item.UnitId.HasValue)
            {
                ModelState.AddModelError($"Items[{i}].Quantity", $"For {item.IngredientName}, enter both quantity and unit, or leave both empty.");
            }

            if(item.UnitId.HasValue)
            {
                //finding the the database unit with the submitted ID
                var unit = model.AvailableUnits.FirstOrDefault(u => u.UnitId == item.UnitId.Value);

                if(unit == null)
                {
                    ModelState.AddModelError($"Items[{i}].UnitId", $"Choose a valid unit for {item.IngredientName}");
                } else if (unit.Abbreviation == "pc" && item.Quantity.HasValue && item.Quantity.Value % 0.25m != 0) {
                    ModelState.AddModelError($"Items[{i}].Quantity", $"Use increments of 0.25 pieces for {item.IngredientName}.");
                }
            }
        }

        // if there are errors, Review is redisplayed, nothing is saved
        if (!ModelState.IsValid) {
            return View("Review", model);
        }

        // making sure the household exists
        if (!await _context.Households.AnyAsync(h => h.HouseholdId == TestHouseholdId))
        {
            ModelState.AddModelError("", "The test houshold can't be found");
            return View("Review", model);
        }

        var dateAdded = DateTime.UtcNow;

        // creating a new entry per submitted review item; assigning householdid to be the test one; later to be changed
        var batches = model.Items.Select(item => new InventoryBatch { 
            IngredientId = item.IngredientId, HouseholdId = TestHouseholdId, Quantity = item.Quantity, 
            UnitId = item.UnitId, ExpirationDate = item.ExpirationDate, DateAdded = dateAdded 
        }).ToList();

        // inserting into the database
        _context.InventoryBatches.AddRange(batches);
        await _context.SaveChangesAsync();

        // adding a success message for the inventory index view
        TempData["InventorySuccess"] = $"{batches.Count} ingredient batches added to inventory.";

        return RedirectToAction(nameof(Index));
    }



    // GET: INVENTORYBATCHS/Details/5
    public async Task<IActionResult> Details(int? ingredientbatchid)
    {
        if (ingredientbatchid == null)
        {
            return NotFound();
        }

        var inventorybatch = await _context.InventoryBatches
            .FirstOrDefaultAsync(m => m.IngredientBatchId == ingredientbatchid);
        if (inventorybatch == null)
        {
            return NotFound();
        }

        return View(inventorybatch);
    }

    // GET: INVENTORYBATCHS/Create
    public async Task<IActionResult> Create()
    {
        var ingredients = await _context.Ingredients.OrderBy(i=>i.IngredientName).ToListAsync();

        ViewBag.IngredientOptions = new SelectList(ingredients, "IngredientId", "IngredientName");
        return View();
    }

    // POST: INVENTORYBATCHS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IngredientBatchId,IngredientId,HouseholdId,UnitId,ExpirationDate,Quantity,DateAdded,Ingredient,Household,Unit")] InventoryBatch inventorybatch)
    {
        if (ModelState.IsValid)
        {
            _context.Add(inventorybatch);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(inventorybatch);
    }

    // opening the edit form for a batch in the test household
    [HttpGet]
    public async Task<IActionResult> Edit(int? ingredientbatchid)
    {
        if (ingredientbatchid == null)
        {
            return NotFound();
        }

        // finding the batchin the test household
        var batch = await _context.InventoryBatches.Include(b => b.Ingredient).FirstOrDefaultAsync(b => b.IngredientBatchId == ingredientbatchid && b.HouseholdId == TestHouseholdId);

        // if no batch exists, return not found
        if (batch == null)
        {
            return NotFound();
        }

        // preparing the information for the view so that the existing values populate the form
        var model = new InventoryEditViewModel
        {
            IngredientBatchId = batch.IngredientBatchId, IngredientName = batch.Ingredient.IngredientName, Quantity = batch.Quantity, 
            UnitId = batch.UnitId, ExpirationDate = batch.ExpirationDate, AvailableUnits = await _context.Units.OrderBy(u => u.UnitName).ToListAsync()
        };


        return View(model);
    }

    // validating and saving changes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? ingredientbatchid, InventoryEditViewModel model)
    {
        // checking that the ID matches the ID from the form
        if(ingredientbatchid == null || ingredientbatchid != model.IngredientBatchId)
        {
            return NotFound();
        }

        // retrieving the existing information from the test household
        var batch = await _context.InventoryBatches.Include(b => b.Ingredient).FirstOrDefaultAsync(b => b.IngredientBatchId == ingredientbatchid && b.HouseholdId == TestHouseholdId);
        
        if(batch == null)
        {
            return NotFound();
        }

        // restoring the name from the database
        model.IngredientName = batch.Ingredient.IngredientName;

        // reloading the dropdown if validation fails
        model.AvailableUnits = await _context.Units.OrderBy( u => u.UnitName).ToListAsync();

        // quantity needs to be above 0
        if(model.Quantity.HasValue && model.Quantity.Value <= 0)
        {
            ModelState.AddModelError(nameof(model.Quantity), "Quantity must be greater than zero.");
        }

        // either both or empty
        if (model.Quantity.HasValue != model.UnitId.HasValue)
        {
            ModelState.AddModelError(nameof(model.Quantity), "Enter both quantity and unit, or leave both empty.");
        }

        // vaidating the selected unit if entered
        if (model.UnitId.HasValue) {
            // finding the unit by Id
            var unit = model.AvailableUnits.FirstOrDefault( u => u.UnitId == model.UnitId.Value);
            if (unit == null)
            {
                ModelState.AddModelError(nameof(model.UnitId), "Choose a valid unit.");
            }
            else if (unit.Abbreviation == "pc" && model.Quantity.HasValue && model.Quantity.Value % 0.25m != 0)
            {
                // quantities for pieces have a 0.25 increment rule (for now)
                ModelState.AddModelError(nameof(model.Quantity), "Use increments of 0.25 pieces.");
            }
        }

        // if validation fails, the page is loaded again with the errors
        if (!ModelState.IsValid)
        {
           return View(model);
        }
        
        //changing these properties to match what was writen by the user
        batch.Quantity = model.Quantity;
        batch.UnitId = model.UnitId;
        batch.ExpirationDate = model.ExpirationDate;

        // saving the changes and redirecting after a successful edit
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // showing the deletion confirmation page
    [HttpGet]
    public async Task<IActionResult> Delete(int? ingredientbatchid)
    {
        if (ingredientbatchid == null)
        {
            return NotFound();
        }

        // finding the batch in the test household and loading the ingredient and unit info
        var batch = await _context.InventoryBatches.Include(b=>b.Ingredient).Include(b=>b.Unit)
            .FirstOrDefaultAsync(b => b.IngredientBatchId == ingredientbatchid.Value && b.HouseholdId == TestHouseholdId);
        
        if (batch == null)
        {
            return NotFound();
        }

        // displaying the view with the delete details
        return View(batch);
    }


    // deleting the batch after confirmation is submitted
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? ingredientbatchid)
    {

        if (ingredientbatchid == null)
        {
            return NotFound();
        }

        // checking the batch and household again
        var batch = await _context.InventoryBatches.FirstOrDefaultAsync(b=>b.IngredientBatchId == ingredientbatchid.Value && b.HouseholdId == TestHouseholdId);
        if (batch == null) { 
            return NotFound(); 
        }

        _context.InventoryBatches.Remove(batch);
        //executing the deletion
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    private bool InventoryBatchExists(int? ingredientbatchid)
    {
        return _context.InventoryBatches.Any(e => e.IngredientBatchId == ingredientbatchid);
    }
}
