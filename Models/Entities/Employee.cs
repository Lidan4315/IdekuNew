// Models/Entities/Employee.cs (Updated)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("employees")]
    public class Employee
    {
        [Key]
        [Column("id", TypeName = "varchar(10)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("name", TypeName = "varchar(100)")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("email", TypeName = "varchar(100)")]
        public string Email { get; set; } = string.Empty;

        [Column("departement_id", TypeName = "varchar(10)")]
        public string? DepartementId { get; set; }
        [ForeignKey("DepartementId")]
        public Departement? Departement { get; set; }

        [Column("divisi_id", TypeName = "varchar(10)")]
        public string? DivisiId { get; set; }
        [ForeignKey("DivisiId")]
        public Divisi? Divisi { get; set; }

        [Required]
        [Column("position_title", TypeName = "varchar(100)")]
        public string PositionTitle { get; set; } = string.Empty;

        [Column("position_level", TypeName = "varchar(10)")]
        public string? PositionLevel { get; set; }

        [Required]
        [Column("employment_status", TypeName = "varchar(20)")]
        public string EmploymentStatus { get; set; } = "Active";

        // Navigation Properties
        public User? User { get; set; }
        public ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
        public ICollection<IdeaMilestone> CreatedMilestones { get; set; } = new List<IdeaMilestone>();
        public ICollection<IdeaMilestone> AssignedMilestones { get; set; } = new List<IdeaMilestone>();
        public ICollection<SavingMonitoring> ReportedMonitoring { get; set; } = new List<SavingMonitoring>();
        public ICollection<SavingMonitoring> ReviewedMonitoring { get; set; } = new List<SavingMonitoring>();
        public ICollection<SystemSetting> UpdatedSettings { get; set; } = new List<SystemSetting>();

        // Computed Properties
        [NotMapped]
        public string Department => Departement?.NamaDepartement ?? "";

        [NotMapped]  
        public string Division => Divisi?.NamaDivisi ?? "";

        [NotMapped]
        public bool IsActive => EmploymentStatus == "Active";
    }
}