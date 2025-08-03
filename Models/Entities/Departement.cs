// Models/Entities/Departement.cs (Updated - removed is_active)
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
        [Column("nama_departement", TypeName = "nvarchar(200)")]
        public string NamaDepartement { get; set; } = string.Empty;

        [Required]
        [Column("divisi_id", TypeName = "varchar(10)")]
        public string DivisiId { get; set; } = string.Empty;
        
        [ForeignKey("DivisiId")]
        public Divisi Divisi { get; set; } = null!;

        // Navigation Properties
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public ICollection<Idea> TargetIdeas { get; set; } = new List<Idea>();
    }
}