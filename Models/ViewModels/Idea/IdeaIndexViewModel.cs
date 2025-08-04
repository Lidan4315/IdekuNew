using Ideku.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ideku.Models.ViewModels.Idea
{
    public class IdeaIndexViewModel
    {
        public List<Entities.Idea> Ideas { get; set; } = new();
        public string CurrentUserName { get; set; } = string.Empty;
        public int TotalIdeas { get; set; }
        public int PendingIdeas { get; set; }
        public int ApprovedIdeas { get; set; }

        // Filter Properties
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Events { get; set; } = new();
        public List<SelectListItem> Statuses { get; set; } = new();

        // Selected filter values
        public int? SelectedCategory { get; set; }
        public int? SelectedEvent { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SearchString { get; set; }

        // Pagination Properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Calculated properties for display
        public int RejectedCount => Ideas.Count(i => i.Status == "Rejected");
        public int InReviewCount => Ideas.Count(i => i.Status == "Under Review");
    }
}