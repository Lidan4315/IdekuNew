using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("divisi")]
    public class Divisi
    {
        [Key]
        [Column("id", TypeName = "varchar(10)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("nama_divisi")]
        public string NamaDivisi { get; set; } = string.Empty;

        // 🔥 NAVIGATION PROPERTIES
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Departement> Departements { get; set; } = new List<Departement>();
    }
}