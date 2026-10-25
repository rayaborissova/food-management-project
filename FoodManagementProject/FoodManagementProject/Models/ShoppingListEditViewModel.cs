using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodManagementProject.Models
{
    public class ShoppingListEditViewModel
    {
        public int ShoppingListItemId { get; set; }

        [ValidateNever]
        public string IngredientName { get; set; } = string.Empty;

        public decimal? Quantity { get; set; }

        public int? UnitId { get; set; }

        [ValidateNever]
        public List<Unit> AvailableUnits { get; set; } = new List<Unit>();
    }
}
