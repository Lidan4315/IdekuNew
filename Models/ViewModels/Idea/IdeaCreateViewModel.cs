using System.ComponentModel.DataAnnotations;

namespace Ideku.Models.ViewModels.Idea
{
    public class IdeaCreateViewModel
    {
        [Required(ErrorMessage = "Badge Number is required")]
        [Display(Name = "Badge Number")]
        public string BadgeNumber { get; set; } = string.Empty;

        // Employee profile fields (read-only, populated by AJAX or current user)
        [Display(Name = "Name")]
        public string EmployeeName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string EmployeeEmail { get; set; } = string.Empty;

        [Display(Name = "Position")]
        public string EmployeePosition { get; set; } = string.Empty;

        [Display(Name = "Division")]
        public string EmployeeDivision { get; set; } = string.Empty;

        [Display(Name = "Department")]
        public string EmployeeDepartment { get; set; } = string.Empty;

        // 🔥 REQUIRED: Target division/department untuk idea
        [Required(ErrorMessage = "To Division is required")]
        [Display(Name = "To Division")]
        public string ToDivision { get; set; } = string.Empty;

        [Required(ErrorMessage = "To Department is required")]
        [Display(Name = "To Department")]
        public string ToDepartment { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int Category { get; set; }

        // 🔥 Event remains OPTIONAL (nullable)
        [Display(Name = "Event")]
        public int? Event { get; set; }

        [Required(ErrorMessage = "Idea Name is required")]
        [StringLength(150, ErrorMessage = "Idea Name cannot exceed 150 characters")]
        [Display(Name = "Idea Name")]
        public string IdeaName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issue Background is required")]
        [StringLength(2000, ErrorMessage = "Issue Background cannot exceed 2000 characters")]
        [Display(Name = "Idea Description")]
        public string IdeaIssueBackground { get; set; } = string.Empty;

        [Required(ErrorMessage = "Solution is required")]
        [StringLength(2000, ErrorMessage = "Solution cannot exceed 2000 characters")]
        [Display(Name = "Solution")]
        public string IdeaSolution { get; set; } = string.Empty;

        [Required(ErrorMessage = "Saving Cost is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Saving Cost must be greater than 0")]
        [Display(Name = "Saving Cost (USD)")]
        public decimal? SavingCost { get; set; }

        // 🔥 Attachment is now REQUIRED
        [Required(ErrorMessage = "At least one attachment is required")]
        [Display(Name = "Attachments")]
        public List<IFormFile> AttachmentFiles { get; set; } = new();
    }
}
