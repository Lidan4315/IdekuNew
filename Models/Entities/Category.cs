// Models/Entities/Category.cs (Updated with description and is_active)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("category")]
    public class Category
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nama_category", TypeName = "nvarchar(100)")]
        public string NamaCategory { get; set; } = string.Empty;

        [Column("description", TypeName = "nvarchar(200)")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Idea> Ideas { get; set; } = new List<Idea>();
    }
}