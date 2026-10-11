using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace FoodManagementProject.Models
{
    //creating the inventory batch properties
    public class InventoryBatch
    {
        [Key]
        public int IngredientBatchId { get; set; }

        public int IngredientId { get; set; }

        public int HouseholdId { get; set; } 

        public int? UnitId { get; set; }

        public DateOnly? ExpirationDate { get; set; }

        public decimal? Quantity { get; set; }

        public DateTime DateAdded { get; set; }

        public Ingredient Ingredient { get; set; } = null!;

        public Household Household { get; set; } = null!;

        public Unit? Unit { get; set; } = null!;
    }
}
