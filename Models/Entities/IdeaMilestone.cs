// Models/Entities/IdeaMilestone.cs (Updated)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("idea_milestones")]
    public class IdeaMilestone
    {
        [Key]
        [Column("id")]
        public long Id { get; set; } // Changed to bigint

        [Required]
        [Column("idea_id")]
        public long IdeaId { get; set; } // Changed to bigint
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;

        [Required]
        [Column("stage")]
        public int Stage { get; set; }

        [Required]
        [Column("milestone_title", TypeName = "nvarchar(200)")]
        public string MilestoneTitle { get; set; } = string.Empty;

        [Column("milestone_description", TypeName = "nvarchar(1000)")]
        public string? MilestoneDescription { get; set; }

        [Column("target_date")]
        public DateTime? TargetDate { get; set; }

        [Column("completion_date")]
        public DateTime? CompletionDate { get; set; }

        [Required]
        [Column("status", TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed, Delayed

        [Column("progress_percentage")]
        public int ProgressPercentage { get; set; } = 0;

        [Column("notes", TypeName = "nvarchar(1000)")]
        public string? Notes { get; set; }

        [Required]
        [Column("created_by_user_id")]
        public long CreatedByUserId { get; set; } // Now references users.id
        [ForeignKey("CreatedByUserId")]
        public User CreatedByUser { get; set; } = null!;

        [Column("assigned_to_user_id")]
        public long? AssignedToUserId { get; set; } // Now references users.id
        [ForeignKey("AssignedToUserId")]
        public User? AssignedToUser { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("updated_date")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties to Employees (through Users)
        [NotMapped]
        public Employee? Creator => CreatedByUser?.Employee;

        [NotMapped]
        public Employee? Assignee => AssignedToUser?.Employee;
    }
}