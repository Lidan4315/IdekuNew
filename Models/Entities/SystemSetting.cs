// Models/Entities/SystemSetting.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("system_settings")]
    public class SystemSetting
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("setting_key", TypeName = "varchar(100)")]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        [Column("setting_value", TypeName = "nvarchar(1000)")]
        public string SettingValue { get; set; } = string.Empty;

        [Required]
        [Column("data_type", TypeName = "varchar(20)")]
        public string DataType { get; set; } = "STRING"; // STRING, NUMBER, BOOLEAN, JSON

        [Column("description", TypeName = "nvarchar(200)")]
        public string? Description { get; set; }

        [Column("is_system")]
        public bool IsSystem { get; set; } = false;

        [Required]
        [Column("updated_by", TypeName = "varchar(10)")]
        public string UpdatedBy { get; set; } = string.Empty;
        [ForeignKey("UpdatedBy")]
        public Employee UpdatedByEmployee { get; set; } = null!;

        [Column("updated_date")]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}