using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodManagementProject.Models
{
    public class ShoppingListReviewViewModel
    {
        public List<ShoppingListReviewItemViewModel> Items { get; set; } = new List<ShoppingListReviewItemViewModel>();

        // will use this to build a dropdown
        [ValidateNever]
        public List<Unit> AvailableUnits { get; set; } = new List<Unit>();

    }

    // holding the selected ingredient and its details
    public class ShoppingListReviewItemViewModel
    {
        public int IngredientId { get; set; }

        [ValidateNever]
        public string IngredientName { get; set; } = string.Empty; // later populate from the database

        public decimal? Quantity { get; set; } // quantity is optional as for mass uploading it will be a tedious process; user can edit it later

        public int? UnitId { get; set; }
    }
}
