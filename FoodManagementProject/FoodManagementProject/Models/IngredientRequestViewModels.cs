using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    // for the form
    public class IngredientRequestFormViewModel
    {
        [Required, StringLength(150)]
        [Display(Name = "Ingredient name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Category (optional)")]
        public int? SuggestedCategoryId { get; set; }

        [StringLength(300)]
        [Display(Name = "Note (optional)")]
        public string? Note {  get; set; }
    }

    // for the request page
    public class IngredientRequestPageViewModel
    {
        public IngredientRequestFormViewModel Form { get; set; } = new();
        public List<IngredientRequest> Requests { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
