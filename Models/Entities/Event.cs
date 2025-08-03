// Models/Entities/Event.cs (Updated with more fields)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("event")]
    public class Event
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nama_event", TypeName = "nvarchar(100)")]
        public string NamaEvent { get; set; } = string.Empty;

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
