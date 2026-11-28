using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Please create a password that meets all requirements below.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z\d]).+$", ErrorMessage = "Please create a password that meets all requirements below.")]  // regex for a letter, a digit, and something that is neither
        [Display(Name = "New password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        //false when the link is unknown, expired, or already used
        public bool TokenIsValid { get; set; }
    }
}
