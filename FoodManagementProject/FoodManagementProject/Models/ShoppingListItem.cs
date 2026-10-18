using System.Diagnostics.Eventing.Reader;

namespace FoodManagementProject.Models
{
    public class ShoppingListItem
    {
        public int ShoppingListItemId {  get; set; }
        public int HouseholdId { get; set; }
        public int IngredientId { get; set; }
        public int? UnitId { get; set; }
        public decimal? Quantity { get; set; }
        public bool IsPurchased { get; set; }
        public string ItemSource { get; set; } = "manual"; //default
        public DateTime? DateAdded { get; set; }

        public Ingredient Ingredient { get; set; } = null!;

        public Household Household { get; set; } = null!;

        public Unit? Unit { get; set; }
    }
}
