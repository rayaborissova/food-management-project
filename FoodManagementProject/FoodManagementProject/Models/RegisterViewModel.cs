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

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)] //min length for password set to 8
        public string Password {  get; set; } = string.Empty;

        [Required,DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage ="The passwords do not match.")] //validation for matching
        [Display(Name = "Confirm password")]
        public string ConfirmPassword {  get; set; } = string.Empty;

    }
}
