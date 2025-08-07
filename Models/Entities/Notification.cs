// Models/Entities/Notification.cs (Updated - no recipient_id, uses idea relation)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("idea_id")]
        public long? IdeaId { get; set; } // Changed to bigint
        [ForeignKey("IdeaId")]
        public Idea? Idea { get; set; }

        [Required]
        [Column("notification_type", TypeName = "varchar(50)")]
        public string NotificationType { get; set; } = string.Empty;

        [Required]
        [Column("title", TypeName = "nvarchar(200)")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("message", TypeName = "nvarchar(1000)")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("priority", TypeName = "varchar(10)")]
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("is_email_sent")]
        public bool IsEmailSent { get; set; } = false;

        [Column("email_sent_date")]
        public DateTime? EmailSentDate { get; set; }

        [Column("action_url", TypeName = "nvarchar(500)")]
        public string? ActionUrl { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("read_date")]
        public DateTime? ReadDate { get; set; }
    }
}