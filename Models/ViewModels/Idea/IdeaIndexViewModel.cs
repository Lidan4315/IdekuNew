using Ideku.Models.Entities;

namespace Ideku.Models.ViewModels.Idea
{
    public class IdeaIndexViewModel
    {
        public List<Entities.Idea> Ideas { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;
        public int TotalIdeas { get; set; }
        public int PendingIdeas { get; set; }
        public int ApprovedIdeas { get; set; }
    }
}