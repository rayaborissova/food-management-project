using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FoodManagementProject.Models
{
    // form for user to fill in to report an ingredient
    public class ReportFormViewModel
    {
        public int IngredientId { get; set; }

        [Required]
        [Display(Name = "What is wrong?")]
        public string Reason {  get; set; } = string.Empty;

        [Display(Name = "Better category (only if the category is wrong)")]
        public int? SuggestedCategoryId { get; set; }

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

    // the decision page, used by moderators
    public class ReportDecisionViewModel
    {
        public int ReportId { get; set; }
        public string? Note { get; set; }
        public string? Resolution { get; set; }

        [ValidateNever]
        public IngredientReport Report { get; set; } = null!;

        // if another there is another open report about the same ingredient
        [ValidateNever]
        public int OtherActiveReports { get; set; }
    }

    // admin's list of reported ingredients
    public class ReportedIngredientsViewModel
    {
        public List<IngredientReport> Forwarded { get; set; } = new();
        public List<IngredientReport> Resolved { get; set; } = new();
    }

}
