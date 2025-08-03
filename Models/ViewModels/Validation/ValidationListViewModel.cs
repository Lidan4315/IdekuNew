using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Ideku.Models.ViewModels.Validation
{
    public class ValidationListViewModel
    {
        public List<ValidationIdeaViewModel> PendingIdeas { get; set; } = new();
        public string ValidatorName { get; set; } = string.Empty;

        // Properties for filters
        public List<SelectListItem> Divisions { get; set; }
        public List<SelectListItem> Departments { get; set; }
        public List<SelectListItem> Statuses { get; set; }
        public List<SelectListItem> Stages { get; set; }

        // Selected filter values
        public string? SelectedDivision { get; set; }
        public string? SelectedDepartment { get; set; }
        public string? SelectedStatus { get; set; }
        public int? SelectedStage { get; set; }
        public string? SearchString { get; set; }

        // Pagination Properties
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Calculated properties for display
        public int TotalPending => PendingIdeas.Count;
        public int SubmittedCount => PendingIdeas.Count(i => i.CurrentStatus == "Submitted");
        public int UnderReviewCount => PendingIdeas.Count(i => i.CurrentStatus == "Under Review");
    }
}
