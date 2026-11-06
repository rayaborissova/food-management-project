using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    // the form for creating a shared household
    public class CreateHouseholdViewModels
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
        public CreateHouseholdViewModels Create { get; set; } = new();
    }


}
