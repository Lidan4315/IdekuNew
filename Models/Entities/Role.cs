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

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}