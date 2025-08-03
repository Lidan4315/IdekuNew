// Models/Entities/Divisi.cs (Updated - removed is_active)
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
        [Column("nama_divisi", TypeName = "nvarchar(200)")]
        public string NamaDivisi { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Departement> Departements { get; set; } = new List<Departement>();
        public ICollection<Idea> TargetIdeas { get; set; } = new List<Idea>();
    }
}