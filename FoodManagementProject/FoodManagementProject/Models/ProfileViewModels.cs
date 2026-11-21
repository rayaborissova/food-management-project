using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    public class ProfileDetailsViewModels
    {
        [Required, StringLength(100)]
        [Display(Name ="Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(255)]
        public string Email { get ; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Current password (only needed to change your email)")]
        public string? CurrentPassword {  get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfilePageViewModel
    {
        public ProfileDetailsViewModels Details { get; set; } = new();
        public ChangePasswordViewModel Password { get; set; } = new();

        // read-only
        [ValidateNever]
        public string Role { get; set; } = string.Empty;

        [ValidateNever]
        public DateTime Registered {  get; set; }

        [ValidateNever]
        public string? HouseholdName { get; set; }

        [ValidateNever]
        public string? HouseholdRole {  get; set; }
    }
}
