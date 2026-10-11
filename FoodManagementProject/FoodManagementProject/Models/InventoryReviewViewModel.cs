using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

// holding the complete review form
namespace FoodManagementProject.Models
{
    public class InventoryReviewViewModel
    {
        public List<InventoryReviewItemViewModel> Items { get; set; } = new List<InventoryReviewItemViewModel>();

        // will use this to build a dropdown
        [ValidateNever]
        public List<Unit> AvailableUnits { get; set; } = new List<Unit>();

    }
}

// holding the selected ingredient and its details
public class InventoryReviewItemViewModel
{
    public int IngredientId { get; set; }

    [ValidateNever]
    public string IngredientName { get; set; } = string.Empty; // later populate from the database

    public decimal? Quantity { get; set; } // quantity is optional as for mass uploading it will be a tedious process; user can edit it later

    public int? UnitId { get; set; } 

    public DateOnly? ExpirationDate { get; set; }
}