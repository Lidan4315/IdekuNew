using System.ComponentModel.DataAnnotations;

namespace Ideku.Models.ViewModels.Idea
{
    public class IdeaCreateViewModel
    {
        [Required(ErrorMessage = "Badge Number is required")]
        [Display(Name = "Badge Number")]
        public string BadgeNumber { get; set; } = string.Empty;

        [Display(Name = "Division")]
        public string? Division { get; set; }

        // 🔥 NEW: Departement dropdown
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Category")]
        public int? Category { get; set; }

        [Display(Name = "Event")]
        public int? Event { get; set; }

        [Required(ErrorMessage = "Idea Name is required")]
        [Display(Name = "Idea Name")]
        [MaxLength(150)]
        public string IdeaName { get; set; } = string.Empty;

        [Display(Name = "Issue Background")]
        public string? IdeaIssueBackground { get; set; }

        [Display(Name = "Solution")]
        public string? IdeaSolution { get; set; }

        [Display(Name = "Saving Cost (USD)")]
        public decimal? SavingCost { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile? AttachmentFile { get; set; }
    }
}