namespace FoodManagementProject.Models
{
    // one card on a dashboard (number, label, short hint, optional link)
    public class SummaryCardViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string HintStatus { get; set; } = "none";
        public string Icon { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

    }

    public class DashboardViewModel
    {
        public string Heading { get; set; } = string.Empty;
        public string Lead { get; set; } = string.Empty;
        public List<SummaryCardViewModel> Cards { get; set; } = new();
    }
}
