namespace FoodManagementProject.Models
{
    //creating the household properties
    public class Household
    {
        public int HouseholdId { get; set; }

        public string HouseholdName { get; set; } = null!;

        public DateTime DateCreated { get; set; }

        public int? PersonalUserId { get; set;  }

        public ICollection<InventoryBatch> InventoryBatches { get; set; } = new List<InventoryBatch>(); //one household can have many inventoyr batches

        public ICollection<ShoppingListItem> ShoppingListItems { get; set; } = new List<ShoppingListItem>();
    }
}
