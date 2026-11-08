using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FoodManagementProject.Models
{
    // the form for creating a shared household
    public class CreateHouseholdViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Household name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name="Copy my current ingredients")]
        public bool CopyInventory { get; set; } //option to copy your ingredients when creating a new household
    }

    // for joining using an invite code
    public class JoinHouseholdViewModel
    {
        [Required, StringLength (12)]
        [Display(Name ="Invite code")]
        public string Code { get; set; } = string.Empty;
    }


    // for the household page
    public class HouseholdIndexViewModel
    {
        public string HouseholdName { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public List<HouseholdMember> Members { get; set; } = new();
        
        // for code invitations to household
        public string? ActiveCode { get; set; }
        public DateTime? CodeExpiresAt { get; set; }
        public int PersonalInventoryCount { get; set; } 

        // for owner additional permissions
        public int CurrentUserId { get; set; }
        public bool IsOwner { get; set; }

        public CreateHouseholdViewModel Create { get; set; } = new();
        public JoinHouseholdViewModel Join { get; set; } = new();
    }


}
