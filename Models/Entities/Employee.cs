using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("employees")]
    public class Employee
    {
        [Key]
        [Column(TypeName = "varchar(10)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Email { get; set; } = string.Empty;

        // 🔥 FOREIGN KEY ke Departement
        [Column("departement_id", TypeName = "varchar(10)")]
        public string? DepartementId { get; set; }
        [ForeignKey("DepartementId")]
        public Departement? Departement { get; set; }

        // 🔥 FOREIGN KEY ke Divisi  
        [Column("divisi_id", TypeName = "varchar(10)")]
        public string? DivisiId { get; set; }
        [ForeignKey("DivisiId")]
        public Divisi? Divisi { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string PositionTitle { get; set; } = string.Empty;

        [Column(TypeName = "varchar(10)")]
        public string? Position_Lvl { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? Emp_Status { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? LdapUser { get; set; }

        // 🔥 COMPUTED PROPERTIES (untuk backward compatibility)
        [NotMapped]
        public string Department => Departement?.NamaDepartement ?? "";

        [NotMapped]  
        public string Division => Divisi?.NamaDivisi ?? "";
    }
}