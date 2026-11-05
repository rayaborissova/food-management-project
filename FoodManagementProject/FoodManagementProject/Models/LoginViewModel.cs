using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    public class LoginViewModel
    {
        // for the login

        [Required,EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in")] // option to stay logged in, more efficient
        public bool RememberMe { get; set; }

    }
}
