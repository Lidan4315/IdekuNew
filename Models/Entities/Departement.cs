using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("departement")]
    public class Departement
    {
        [Key]
        [Column("id", TypeName = "varchar(10)")]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("nama_departement")]
        public string NamaDepartement { get; set; } = string.Empty;

        [Required]
        [Column("divisi_id", TypeName = "varchar(10)")]
        public string DivisiId { get; set; } = string.Empty;
        
        [ForeignKey("DivisiId")]
        public Divisi Divisi { get; set; } = null!;

        // 🔥 NAVIGATION PROPERTIES
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}