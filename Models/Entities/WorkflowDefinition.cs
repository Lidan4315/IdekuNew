// Models/Entities/WorkflowDefinition.cs (NEW)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("workflow_definitions")]
    public class WorkflowDefinition
    {
        [Key]
        [Column("id", TypeName = "varchar(20)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("name", TypeName = "nvarchar(100)")]
        public string Name { get; set; } = string.Empty;

        [Column("description", TypeName = "nvarchar(500)")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<WorkflowStage> WorkflowStages { get; set; } = new List<WorkflowStage>();
        public ICollection<WorkflowCondition> WorkflowConditions { get; set; } = new List<WorkflowCondition>();
        public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    }
}