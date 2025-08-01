using System;

namespace Ideku.Models.ViewModels.Validation
{
    public class ValidationIdeaViewModel
    {
        public int Id { get; set; }
        public string? IdeaName { get; set; }
        public string? InitiatorName { get; set; }
        public string? DivisionName { get; set; }
        public string? DepartmentName { get; set; }
        public string? CategoryName { get; set; }
        public int? CurrentStage { get; set; }
        public string? CurrentStatus { get; set; }
        public decimal? SavingCost { get; set; }
        public DateTime SubmittedDate { get; set; }
    }
}
