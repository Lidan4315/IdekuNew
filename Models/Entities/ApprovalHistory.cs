// Models/Entities/ApprovalHistory.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("approval_history")]
    public class ApprovalHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("idea_id")]
        public int IdeaId { get; set; }
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;

        [Required]
        [Column("stage")]
        public int Stage { get; set; }

        [Required]
        [Column("action", TypeName = "varchar(20)")]
        public string Action { get; set; } = string.Empty; // APPROVE, REJECT, REQUEST_INFO, MILESTONE

        [Required]
        [Column("approver_id", TypeName = "varchar(10)")]
        public string ApproverId { get; set; } = string.Empty;
        [ForeignKey("ApproverId")]
        public Employee Approver { get; set; } = null!;

        [Required]
        [Column("approver_role_id", TypeName = "varchar(10)")]
        public string ApproverRoleId { get; set; } = string.Empty;
        [ForeignKey("ApproverRoleId")]
        public Role ApproverRole { get; set; } = null!;

        [Column("comments", TypeName = "nvarchar(1000)")]
        public string? Comments { get; set; }

        [Column("validated_saving_cost", TypeName = "decimal(18,2)")]
        public decimal? ValidatedSavingCost { get; set; }

        [Column("attachment_files", TypeName = "nvarchar(max)")]
        public string? AttachmentFiles { get; set; }

        [Column("action_date")]
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        [Column("ip_address", TypeName = "varchar(45)")]
        public string? IpAddress { get; set; }

        [Column("user_agent", TypeName = "nvarchar(500)")]
        public string? UserAgent { get; set; }
    }
}