// Models/Entities/Permission.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("permissions")]
    public class Permission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("permission_name", TypeName = "varchar(100)")]
        public string PermissionName { get; set; } = string.Empty;

        [Column("description", TypeName = "varchar(200)")]
        public string? Description { get; set; }

        [Required]
        [Column("module", TypeName = "varchar(50)")]
        public string Module { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}