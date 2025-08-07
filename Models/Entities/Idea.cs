// Models/Entities/Idea.cs (Complete with new schema)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("ideas")]
    public class Idea
    {
        [Key]
        [Column("id")]
        public long Id { get; set; } // Auto-increment bigint

        [Required]
        [Column("initiator_user_id")]
        public long InitiatorUserId { get; set; }
        [ForeignKey("InitiatorUserId")]
        public User Initiator { get; set; } = null!;

        [Required]
        [Column("target_division_id", TypeName = "varchar(10)")]
        public string TargetDivisionId { get; set; } = string.Empty;
        [ForeignKey("TargetDivisionId")]
        public Divisi TargetDivision { get; set; } = null!;

        [Required]
        [Column("target_department_id", TypeName = "varchar(10)")]
        public string TargetDepartmentId { get; set; } = string.Empty;
        [ForeignKey("TargetDepartmentId")]
        public Departement TargetDepartment { get; set; } = null!;

        [Required]
        [Column("category_id")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        [Column("event_id")]
        public int? EventId { get; set; }
        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        [Column("workflow_definition_id", TypeName = "varchar(20)")]
        public string? WorkflowDefinitionId { get; set; }
        [ForeignKey("WorkflowDefinitionId")]
        public WorkflowDefinition? WorkflowDefinition { get; set; }

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

        // ===== COMPUTED PROPERTIES FOR DISPLAY =====
        
        /// <summary>
        /// User-friendly display ID (IMS-XXXXXXX format)
        /// Example: ID 1 → "IMS-0000001"
        /// </summary>
        [NotMapped]
        public string DisplayId => $"IMS-{Id:D7}";

        /// <summary>
        /// Short display ID for compact views
        /// Example: ID 1 → "IMS-001"
        /// </summary>
        [NotMapped]
        public string ShortDisplayId => $"IMS-{Id:D3}";

        /// <summary>
        /// Display ID with hash symbol for UI
        /// Example: ID 1 → "#IMS-0000001"
        /// </summary>
        [NotMapped]
        public string HashDisplayId => $"#{DisplayId}";

        /// <summary>
        /// Formatted saving cost for display
        /// </summary>
        [NotMapped]
        public string FormattedSavingCost => SavingCost.ToString("N0");

        /// <summary>
        /// Formatted validated saving cost for display
        /// </summary>
        [NotMapped]
        public string FormattedValidatedSavingCost => ValidatedSavingCost?.ToString("N0") ?? "-";

        /// <summary>
        /// Status class for UI styling
        /// </summary>
        [NotMapped]
        public string StatusClass => Status switch
        {
            "Submitted" => "bg-primary",
            "Under Review" => "bg-warning",
            "Approved" => "bg-success",
            "Rejected" => "bg-danger",
            "Completed" => "bg-success",
            _ => "bg-secondary"
        };

        /// <summary>
        /// Status icon for UI
        /// </summary>
        [NotMapped]
        public string StatusIcon => Status switch
        {
            "Submitted" => "bi-upload",
            "Under Review" => "bi-clock",
            "Approved" => "bi-check-circle",
            "Rejected" => "bi-x-circle",
            "Completed" => "bi-trophy",
            _ => "bi-info-circle"
        };

        /// <summary>
        /// Initiator name for display
        /// </summary>
        [NotMapped]
        public string InitiatorName => Initiator?.Employee?.Name ?? "Unknown";

        /// <summary>
        /// Category name for display
        /// </summary>
        [NotMapped]
        public string CategoryName => Category?.NamaCategory ?? "";

        /// <summary>
        /// Event name for display
        /// </summary>
        [NotMapped]
        public string EventName => Event?.NamaEvent ?? "";

        /// <summary>
        /// Division name for display
        /// </summary>
        [NotMapped]
        public string DivisionName => TargetDivision?.NamaDivisi ?? "";

        /// <summary>
        /// Department name for display
        /// </summary>
        [NotMapped]
        public string DepartmentName => TargetDepartment?.NamaDepartement ?? "";
    }
}