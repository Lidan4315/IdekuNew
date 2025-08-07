// Models/Entities/User.cs (Updated)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public long Id { get; set; } // Changed to bigint

        [Required]
        [Column("employee_id", TypeName = "varchar(10)")]
        public string EmployeeId { get; set; } = string.Empty;

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        [Required]
        [Column("role_id", TypeName = "varchar(10)")]
        public string RoleId { get; set; } = string.Empty;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [Required]
        [Column("username", TypeName = "varchar(50)")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("name", TypeName = "varchar(150)")]
        public string Name { get; set; } = string.Empty;

        [Column("is_acting")]
        public bool IsActing { get; set; } = false;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<Idea> InitiatedIdeas { get; set; } = new List<Idea>();
        public ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
        public ICollection<IdeaMilestone> CreatedMilestones { get; set; } = new List<IdeaMilestone>();
        public ICollection<IdeaMilestone> AssignedMilestones { get; set; } = new List<IdeaMilestone>();
        public ICollection<SavingMonitoring> ReportedMonitoring { get; set; } = new List<SavingMonitoring>();
        public ICollection<SavingMonitoring> ReviewedMonitoring { get; set; } = new List<SavingMonitoring>();
        public ICollection<SystemSetting> UpdatedSettings { get; set; } = new List<SystemSetting>();
    }
}