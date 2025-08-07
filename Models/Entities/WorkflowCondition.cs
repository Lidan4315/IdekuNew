// Models/Entities/WorkflowCondition.cs (NEW)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("workflow_conditions")]
    public class WorkflowCondition
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("workflow_id", TypeName = "varchar(20)")]
        public string WorkflowId { get; set; } = string.Empty;

        [Required]
        [Column("condition_type", TypeName = "varchar(50)")]
        public string ConditionType { get; set; } = string.Empty; // SAVING_COST, CATEGORY, DIVISION, DEPARTMENT, EVENT

        [Required]
        [Column("operator", TypeName = "varchar(10)")]
        public string Operator { get; set; } = string.Empty; // >=, <=, =, !=, IN, NOT_IN

        [Required]
        [Column("condition_value", TypeName = "nvarchar(500)")]
        public string ConditionValue { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("WorkflowId")]
        public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    }
}