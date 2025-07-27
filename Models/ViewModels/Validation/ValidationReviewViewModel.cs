// Models/ViewModels/Validation/ValidationReviewViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Ideku.Models.ViewModels.Validation
{
    public class ValidationReviewViewModel
    {
        public int IdeaId { get; set; }
        public string IdeaName { get; set; } = string.Empty;
        public string? IdeaIssueBackground { get; set; }
        public string? IdeaSolution { get; set; }
        public decimal? SavingCost { get; set; }
        public string? AttachmentFile { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string SubmitterName { get; set; } = string.Empty;
        public string SubmitterEmail { get; set; } = string.Empty;
        public string SubmitterId { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? EventName { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }

        [Display(Name = "Validation Comments")]
        public string? ValidationComments { get; set; }

        [Display(Name = "Validated Saving Cost (USD)")]
        public decimal? ValidatedSavingCost { get; set; }

        [Display(Name = "Rejection Reason")]
        public string? RejectionReason { get; set; }

        [Display(Name = "Information Request")]
        public string? InformationRequest { get; set; }
    }

    public class ValidationListViewModel
    {
        public List<Ideku.Models.Entities.Idea> PendingIdeas { get; set; } = new();
        public string ValidatorName { get; set; } = string.Empty;
        public int TotalPending => PendingIdeas.Count;
        public int SubmittedCount => PendingIdeas.Count(i => i.CurrentStatus == "Submitted");
        public int UnderReviewCount => PendingIdeas.Count(i => i.CurrentStatus == "Under Review");
    }

    public class ValidationActionViewModel
    {
        public int IdeaId { get; set; }
        public string Action { get; set; } = string.Empty; // "approve", "reject", "more_info"
        public string? Comments { get; set; }
        public decimal? ValidatedSavingCost { get; set; }
    }
}