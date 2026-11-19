namespace FoodManagementProject.Models
{
    public class PasswordReset 
    {
        public int ResetId { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime DateCreated { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public AppUser User { get; set; } = null!;
    }
}
