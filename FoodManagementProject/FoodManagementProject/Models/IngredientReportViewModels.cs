using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodManagementProject.Models
{
    // form for user to fill in to report an ingredient
    public class ReportViewModel
    {
        public int IngredientId { get; set; }

        [Required]
        [Display(Name = "What is wrong?")]
        public string Reason {  get; set; } = string.Empty;

        [Display(Name = "Better category (only if the category is wrong)")]
        public string? SuggestedCategoryId { get; set; }

        [StringLength(300)]
        [Display(Name = "Note")]
        public string? Note { get; set; }

        [ValidateNever]
        public string IngredientName { get; set; } = string.Empty;

        [ValidateNever]
        public string CurrentCategoryName {  get; set; } = string.Empty;

        [ValidateNever]
        public List<Category> Categories { get; set; } = new();
    }
}
