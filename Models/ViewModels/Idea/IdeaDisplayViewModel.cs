// Models/ViewModels/IdeaDisplayViewModel.cs (Supporting ViewModel)
namespace Ideku.Models.ViewModels.Idea
{
    public class IdeaDisplayViewModel
    {
        public long Id { get; set; }
        public string DisplayId { get; set; } = string.Empty;
        public string IdeaName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string InitiatorName { get; set; } = string.Empty;
        public decimal SavingCost { get; set; }
        public decimal? ValidatedSavingCost { get; set; }
        public int CurrentStage { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string? CategoryName { get; set; }
        public string? DivisionName { get; set; }
        public string? DepartmentName { get; set; }

        // Helper properties for UI
        public string StatusClass => Status switch
        {
            "Submitted" => "bg-primary",
            "Under Review" => "bg-warning",
            "Approved" => "bg-success",
            "Rejected" => "bg-danger",
            "Completed" => "bg-success",
            _ => "bg-secondary"
        };

        public string StatusIcon => Status switch
        {
            "Submitted" => "bi-upload",
            "Under Review" => "bi-clock",
            "Approved" => "bi-check-circle",
            "Rejected" => "bi-x-circle",
            "Completed" => "bi-trophy",
            _ => "bi-info-circle"
        };

        public string FormattedSavingCost => SavingCost.ToString("N0");
        public string FormattedValidatedSavingCost => ValidatedSavingCost?.ToString("N0") ?? "-";
        public string FormattedSubmittedDate => SubmittedDate.ToString("MMM dd, yyyy");
        public string ShortFormattedDate => SubmittedDate.ToString("MMM dd");
    }
}