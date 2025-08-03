// Models/Entities/Idea.cs (Updated with new structure and custom ID)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("ideas")]
    public class Idea
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("idea_code", TypeName = "varchar(20)")]
        public string IdeaCode { get; set; } = string.Empty; // IMS-00001 format

        [Required]
        [Column("initiator_id", TypeName = "varchar(10)")]
        public string InitiatorId { get; set; } = string.Empty;
        [ForeignKey("InitiatorId")]
        public Employee? Initiator { get; set; }

        [Required]
        [Column("target_division_id", TypeName = "varchar(10)")]
        public string TargetDivisionId { get; set; } = string.Empty;
        [ForeignKey("TargetDivisionId")]
        public Divisi? TargetDivision { get; set; }

        [Required]
        [Column("target_department_id", TypeName = "varchar(10)")]
        public string TargetDepartmentId { get; set; } = string.Empty;
        [ForeignKey("TargetDepartmentId")]
        public Departement? TargetDepartment { get; set; }

        [Required]
        [Column("category_id")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Column("event_id")]
        public int? EventId { get; set; }
        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        // Core Content
        [Required]
        [Column("idea_name", TypeName = "nvarchar(150)")]
        public string IdeaName { get; set; } = string.Empty;

        [Required]
        [Column("issue_background", TypeName = "nvarchar(2000)")]
        public string IssueBackground { get; set; } = string.Empty;

        [Required]
        [Column("solution", TypeName = "nvarchar(2000)")]
        public string Solution { get; set; } = string.Empty;

        // Financial Impact
        [Required]
        [Column("saving_cost", TypeName = "decimal(18,2)")]
        public decimal SavingCost { get; set; }

        [Column("validated_saving_cost", TypeName = "decimal(18,2)")]
        public decimal? ValidatedSavingCost { get; set; }

        // Workflow Status
        [Column("current_stage")]
        public int CurrentStage { get; set; } = 0;

        [Required]
        [Column("status", TypeName = "varchar(20)")]
        public string Status { get; set; } = "Submitted";

        [Required]
        [Column("workflow_type", TypeName = "varchar(20)")]
        public string WorkflowType { get; set; } = string.Empty; // STANDARD or HIGH_VALUE

        [Column("max_stage")]
        public int MaxStage { get; set; }

        // Files
        [Column("attachment_files", TypeName = "nvarchar(max)")]
        public string? AttachmentFiles { get; set; }

        // Timestamps
        [Column("submitted_date")]
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        [Column("updated_date")]
        public DateTime? UpdatedDate { get; set; }

        [Column("completed_date")]
        public DateTime? CompletedDate { get; set; }

        // Additional
        [Column("reject_reason", TypeName = "nvarchar(1000)")]
        public string? RejectReason { get; set; }

        [Column("implementation_notes", TypeName = "nvarchar(max)")]
        public string? ImplementationNotes { get; set; }

        // Navigation Properties
        public ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
        public ICollection<IdeaMilestone> Milestones { get; set; } = new List<IdeaMilestone>();
        public ICollection<SavingMonitoring> SavingMonitoring { get; set; } = new List<SavingMonitoring>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}