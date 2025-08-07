// Models/Entities/WorkflowStage.cs (NEW)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("workflow_stages")]
    public class WorkflowStage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("workflow_id", TypeName = "varchar(20)")]
        public string WorkflowId { get; set; } = string.Empty;

        [Required]
        [Column("stage_id", TypeName = "varchar(10)")]
        public string StageId { get; set; } = string.Empty;

        [Column("sequence_number")]
        public int SequenceNumber { get; set; }

        [Column("is_mandatory")]
        public bool IsMandatory { get; set; } = true;

        [Column("is_parallel")]
        public bool IsParallel { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("WorkflowId")]
        public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

        [ForeignKey("StageId")]
        public Stage Stage { get; set; } = null!;
    }
}