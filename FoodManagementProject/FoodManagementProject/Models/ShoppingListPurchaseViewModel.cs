using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodManagementProject.Models
{
    public class ShoppingListPurchaseViewModel
    {
        //holding the checked entries that are being transfered to inventory
        public List<ShoppingListPurchaseItemViewModel> Items { get; set; } = new List<ShoppingListPurchaseItemViewModel>();

        [ValidateNever]
        public List<Unit> AvailableUnits { get; set; } = new List<Unit>();  
    }

    public class ShoppingListPurchaseItemViewModel
    {
        public int ShoppingListItemId { get; set; }

        [ValidateNever]
        public string IngredientName { get; set; } = string.Empty;

        [ValidateNever]
        public string UnitName { get; set; } = string.Empty;

        public decimal? Quantity { get; set; }
        public int? UnitId { get; set; }
        public DateOnly? ExpirationDate { get; set; }

    }
}
