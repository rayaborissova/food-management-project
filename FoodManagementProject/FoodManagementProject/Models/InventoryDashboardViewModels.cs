namespace FoodManagementProject.Models
{
    // one batch inside an expanded ingredient row
    public class InventoryBatchRowViewModel
    {
        public int IngredientBatchId { get; set; }
        public string QuantityText { get; set; } = string.Empty;
        public string ExpiryText { get; set; } = string.Empty;
        public string Status { get; set; } = "none"; // expired, soon, ok or none
    }

    // one row on the dashboard, an ingredient with all its batches
    public class InventoryGroupViewModel
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Initial { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string MetaText { get; set; } = string.Empty; // example Dairy · 2 batches · 1.5 l total
        public string Status { get; set; } = "none"; // worst status among the batches
        public string ExpiryText { get; set; } = string.Empty; // taken from earliest expiry
        public int SortDays { get; set; } // days until the earliest expiry
        public List<InventoryBatchRowViewModel> Batches { get; set; } = new();
    }

    public class InventoryCategoryTab
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class InventoryIndexViewModel
    {
        public List<InventoryGroupViewModel> Groups { get; set; } = new();
        public List<InventoryCategoryTab> Categories { get; set; } = new();
        public int ExpireCount { get; set; }
        public int SoonCount { get; set; }
        public int NoDateCount { get; set; }
        public string HouseholdName { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public int MemberCount { get; set; }
    }


}
