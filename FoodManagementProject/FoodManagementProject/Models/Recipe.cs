namespace FoodManagementProject.Models
{

    public class RecipeVisibilities
    {
        public const string Private = "private";
        public const string Public = "public";
    }
    public class Recipe
    {
        public int RecipeId { get; set; }
        public int AuthorId { get; set; }
        public string Title { get; set; } = null!;
        public int Servings { get; set; } = 2;
        public int? PrepMinutes { get; set; }
        public int? CookMinutes { get; set; }
        public string Instructions { get; set; } = null!;
        public string Visibility { get; set; } = RecipeVisibilities.Private;
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }

        public AppUser Author { get; set; } = null;
        public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    }
}
