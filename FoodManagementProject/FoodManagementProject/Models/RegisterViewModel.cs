using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    public class RegisterViewModel
    {
        //for user registration
        //all required, set for the proper data type

        [Required, StringLength(100)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(255)] 
        public string Email {  get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Please create a password that meets all requirements below.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).+$", ErrorMessage = "Please create a password that meets all requirements below.")]  // regex for a letter, a digit, and something that is neither
        public string Password { get; set; } = string.Empty;

    }
}
