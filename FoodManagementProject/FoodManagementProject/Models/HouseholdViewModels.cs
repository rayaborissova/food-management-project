using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    // the form for creating a shared household
    public class CreateHouseholdViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Household name")]
        public string Name { get; set; } = string.Empty;
    }

    // for the household page
    public class HouseholdIndexViewModel
    {
        public string HouseholdName { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public List<HouseholdMember> Members { get; set; } = new();
        public CreateHouseholdViewModel Create { get; set; } = new();
    }


}
