// Models/Entities/RolePermission.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("role_permissions")]
    public class RolePermission
    {
        [Column("role_id", TypeName = "varchar(10)")]
        public string RoleId { get; set; } = string.Empty;

        [Column("permission_id")]
        public int PermissionId { get; set; }

        [Column("granted_at")]
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;

        [ForeignKey("PermissionId")]
        public Permission Permission { get; set; } = null!;
    }
}