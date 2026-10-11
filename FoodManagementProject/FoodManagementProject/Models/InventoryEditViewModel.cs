using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodManagementProject.Models
{
    public class InventoryEditViewModel
    {
        public int IngredientBatchId { get; set; }

        [ValidateNever]
        public string IngredientName { get; set; }
        public decimal? Quantity { get; set; }
        public int? UnitId { get; set; }
        public DateOnly? ExpirationDate { get; set; }

        //adding the unit dropdown
        [ValidateNever]
        public List<Unit> AvailableUnits { get; set; } = new List<Unit>();
    }
}
