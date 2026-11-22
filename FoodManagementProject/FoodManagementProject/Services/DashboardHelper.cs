namespace FoodManagementProject.Services
{
    public static class DashboardHelper
    {
        // an item waiting longer than this counts as stale
        public const int StaleDays = 7;

        public const int AttentionDays = 3;

        // the hint under a "waiting" card (how long the oldest item has waited, coloured by urgency)
        public static (string Hint, string Status) WaitingHint(IReadOnlyCollection<DateTime> dates, DateTime now)
        {
            if (dates.Count == 0)
            {
                return ("All caught up", "ok");
            }

            var stale = dates.Count(d => (now - d).TotalDays > StaleDays);
            if (stale > 0)
            {
                return ($"{stale} waiting more than {StaleDays} days", "expired");
            }

            var oldest = (int)(now - dates.Min()).TotalDays;
            return ($"Oldest: {oldest} {(oldest == 1 ? "day" : "days")}", oldest >= AttentionDays ? "soon" : "ok");
        }

    }
}
