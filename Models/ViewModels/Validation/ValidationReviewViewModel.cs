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

}
