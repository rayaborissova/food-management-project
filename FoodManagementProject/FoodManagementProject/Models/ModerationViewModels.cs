using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


namespace FoodManagementProject.Models

{
    public class ModerationReviewViewModel
    {
        public int RequestId { get; set; }
        public string? FinalName { get; set; }
        public int? CategoryId { get; set; }
        public string? ReviewNote { get; set; }

        [ValidateNever]
        public IngredientRequest Request { get; set; } = null!;

        [ValidateNever]
        public List<Category> Categories { get; set; } = new();

        // if something similar exists, to help spot duplicates
        [ValidateNever]
        public List<Ingredient> Similar {  get; set; } = new();
    }
}
