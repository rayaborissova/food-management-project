using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace FoodManagementProject.Models
{
    public class ReportStatuses 
    {
        public const string Open = "open";
        public const string Dismissed = "dismissed";
        public const string Forwarded = "forwarded";
        public const string Resolved = "resolved";
    }

    public static class ReportResolutions
    {
        public const string Fixed = "fixed";
        public const string NoChange = "no_change";
    }

    public static class ReportReasons
    {
        public const string WrongName = "wrong_name";
        public const string WrongCategory = "wrong_category";
        public const string Duplicate = "duplicate";
        public const string Other = "other";

        public static readonly (string Value, string Label)[] All =
        {
            (WrongName, "Wrong name or typo"),
            (WrongCategory, "Wrong category"),
            (Duplicate, "Duplicate of another ingredient"),
            (Other, "Something else")
        };

        public static string Label(string value)
        {
            return All.FirstOrDefault(r => r.Value == value).Label ?? value;
        }
    }

    public class IngredientReport
    {
        public int ReportId { get; set; }
        public int IngredientId { get; set; }
        public int ReportedBy { get; set; }
        public string Reason { get; set; } = null!;
        public string? Note { get; set; }
        public int? SuggestedCategoryId { get; set; }
        public string ReportStatus { get; set; } = ReportStatuses.Open;
        public DateTime DateCreated { get; set; }
        public int? ReviewedBy { get; set; }
        public DateTime? DateReviewed { get; set; }
        public string? ReviewNote { get; set; }
        public int? ResolvedBy { get; set; }
        public DateTime? DateResolved { get; set; }
        public string? Resolution { get; set; }
        public string? ResolutionNote { get; set; }

        public Ingredient Ingredient { get; set; } = null!;
        public AppUser Reporter { get; set; } = null!;
        public AppUser? Reviewer { get; set; }

        public AppUser? Resolver { get; set; }
        public Category? SuggestedCategory { get; set; }

    }

}
