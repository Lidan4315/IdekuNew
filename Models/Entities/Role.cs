// Models/Entities/Role.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [Column("id", TypeName = "varchar(10)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("role_name", TypeName = "varchar(50)")]
        public string RoleName { get; set; } = string.Empty;

        [Column("description", TypeName = "varchar(100)")]
        public string? Description { get; set; }

        [Column("approval_level")]
        public int ApprovalLevel { get; set; }

        [Column("can_approve_standard")]
        public bool CanApproveStandard { get; set; } = false;

        [Column("can_approve_high_value")]
        public bool CanApproveHighValue { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}