using System;
using System.Collections.Generic;

namespace FoodManagementProject.Models;

// creating the ingredient properties
public partial class Ingredient
{
    public int IngredientId { get; set; }

    public int CategoryId { get; set; }

    public string IngredientName { get; set; } = null!;

    public string IngredientNameNormalized { get; set; } = null!;

    public string? IngredientSource { get; set; } 

    public string? Status { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>(); //one ingredient can be in many inventory batches

}
