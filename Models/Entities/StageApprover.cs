// Models/Entities/StageApprover.cs (NEW)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("stage_approvers")]
    public class StageApprover
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("stage_id", TypeName = "varchar(10)")]
        public string StageId { get; set; } = string.Empty;

        [Required]
        [Column("role_id", TypeName = "varchar(10)")]
        public string RoleId { get; set; } = string.Empty;

        [Column("is_primary")]
        public bool IsPrimary { get; set; } = true;

        [Column("approval_order")]
        public int ApprovalOrder { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("StageId")]
        public Stage Stage { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;
    }
}