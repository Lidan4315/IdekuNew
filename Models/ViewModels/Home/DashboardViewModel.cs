namespace Ideku.Models.ViewModels.Home
{
    public class DashboardViewModel
    {
        public int TotalIdeas { get; set; }
        public int PendingIdeas { get; set; }
        public int ApprovedIdeas { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        public List<RecentIdea> RecentIdeas { get; set; } = new();
    }

    public class RecentIdea
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
    }
}