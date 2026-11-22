using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodManagementProject.Models
{
    // one ingredient line of the recipe form
    public class RecipeIngredientLineViewModel
    {
        public int IngredientId { get; set; }

        [ValidateNever]
        public string IngredientName { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public int? UnitId { get; set; }
        public string? Note { get; set; }
    }

    // the create and edit form
    public class RecipeFormViewModels
    {
        public int? RecipeId { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Range(1, 100)]
        public int Servings { get; set; } = 2;

        [Range(1, 1500)]
        [Display(Name = "Preparation (minutes)")]
        public int? PrepMinutes { get; set; }

        [Range(1, 1500)]
        [Display(Name = "Cooking (minutes)")]
        public int? CookMinutes { get; set; }

        [Required, StringLength(8000)]
        public string Instructions { get; set; } = string.Empty;

        [Required]
        public string Visibility { get; set; } = RecipeVisibilities.Private;

        public List<RecipeIngredientLineViewModel> Lines { get; set; } = new();

        [ValidateNever]
        public List<Unit> Units { get; set; } = new();
    }

    // one row in a list of recipes
    public class RecipeCardViewModel
    {
        public int RecipeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Visibility { get; set; } = string.Empty;
        public int Servings { get; set; } 
        public int TotalMinutes { get; set; } 
        public int IngredientCount { get; set; } 
        public int HaveCount { get; set; } // how many are present in the inevntory
        public DateTime DateUpdated { get; set; }
    }

    public class RecipeDetailsViewModel
    {
        public Recipe Recipe { get; set; } = null!;
        public bool IsAuthor { get; set; }

        // the ingredients currently available in the inventory
        public HashSet<int> OwnedIngredientIds { get; set; } = new();

        public int HaveCount => Recipe.Ingredients.Count(i => OwnedIngredientIds.Contains(i.IngredientId));
        public int MissingCount => Recipe.Ingredients.Count - HaveCount;
    }
}
