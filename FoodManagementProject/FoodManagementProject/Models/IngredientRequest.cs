namespace FoodManagementProject.Models
{
    
    // status options for the requests
    public static class RequestStatuses
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
    }
    public class IngredientRequest
    {
        public int RequestId { get; set; }
        public int RequestedBy { get; set; }
        public string RequestedName { get; set; } = null!;
        public int? SuggestedCategoryId { get; set; }
        public string? Note { get; set; }
        public string RequestStatus { get; set; } = RequestStatuses.Pending;
        public DateTime DateCreated { get; set; }
        public int? ReviewedBy { get; set; }
        public DateTime? DateReviewed { get; set; }
        public string? ReviewNote { get; set; }
        public int? IngredientId { get; set; }

        public AppUser Requester { get; set; } = null!;
        public AppUser? Reviewer { get; set; }
        public Category? SuggestedCategory { get; set; }
        public Ingredient? Ingredient { get; set; }
    }
}
