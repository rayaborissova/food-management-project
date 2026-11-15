namespace FoodManagementProject.Models
{
    public class AdminUserViewListModel
    {
        public string? Search {  get; set; }
        public int CurrentUserId { get; set; }
        public List<AppUser> Users { get; set; } = new();
    }
}
