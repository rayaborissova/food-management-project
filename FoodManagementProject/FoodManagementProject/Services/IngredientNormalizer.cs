using System.Security.Cryptography.X509Certificates;

namespace FoodManagementProject.Services
{
    public class IngredientNormalizer
    {
        // displaying the form 
        public static string Clean(string name)
        {
            return string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        }

        // storing search form in ingredient_name_normalized
        public static string Normalize(string name) 
        {
            return Clean(name).ToLowerInvariant();
        }
    
    }
}
