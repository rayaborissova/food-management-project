namespace FoodManagementProject.Models
{
    
    //creating the category properties
    public class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;

        public ICollection<Ingredient> Ingredients {  get; set; } = new List<Ingredient>(); // one category can have many ingredients
    }
}
