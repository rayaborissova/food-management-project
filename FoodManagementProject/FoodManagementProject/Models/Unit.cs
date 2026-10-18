namespace FoodManagementProject.Models
{
    //creating the unit properties
    public class Unit
    {
        public int UnitId { get; set; }

        public string UnitName { get; set; } = null!;

        public string Abbreviation { get; set; } = null!;

        public ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>(); //one unit can be in many inventory batches 

        public ICollection<ShoppingListItem> ShoppingListItems { get; set; } = new List<ShoppingListItem>();
    }
}
