// Models/Entities/User.cs (Updated with role relationship)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

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
    }
}