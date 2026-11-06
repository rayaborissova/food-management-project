namespace FoodManagementProject.Models
{
    public class HouseholdMember
    { 
        public int UserId { get; set; }
        public int HouseholdId { get; set; }
        public string HouseholdRole { get; set; } = "member";
        public DateTime DateJoined { get; set; }
        public AppUser User { get; set; } = null!;
        public Household Household { get; set; } = null!;

    }
}
