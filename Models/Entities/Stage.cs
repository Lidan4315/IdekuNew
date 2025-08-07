// Models/Entities/Stage.cs (NEW)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("stages")]
    public class Stage
    {
        [Key]
        [Column("id", TypeName = "varchar(10)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("name", TypeName = "nvarchar(100)")]
        public string Name { get; set; } = string.Empty;

        [Column("description", TypeName = "nvarchar(500)")]
        public string? Description { get; set; }

        [Column("default_timeout_days")]
        public int? DefaultTimeoutDays { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<StageApprover> StageApprovers { get; set; } = new List<StageApprover>();
        public ICollection<WorkflowStage> WorkflowStages { get; set; } = new List<WorkflowStage>();
    }
}