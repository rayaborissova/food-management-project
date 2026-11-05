namespace FoodManagementProject.Models
{
    public class AppUser
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null;
        public string PasswordHash { get; set; } = null;
        public string DisplayName { get; set; } = null;
        public string UserRole { get; set; } = "regular";
        public DateTime DateCreated { get; set; }
        public HouseholdMember? Membership { get; set; }
    }
}
