namespace FoodManagementProject.Models
{
    public class HouseholdInvite
    {
        public int InviteId { get; set; }
        public int HouseholdId { get; set; }
        public int CreatedBy { get; set; } // can be created by all members, not just the owner
        public string Code { get; set; } = null!;
        public DateTime DateCreated { get; set; }
        public DateTime ExpireAt { get; set; }
        public Household Household { get; set; } = null!; // the invite can be used by one household
    }
}
