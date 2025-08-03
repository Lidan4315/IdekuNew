// Models/Entities/SavingMonitoring.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ideku.Models.Entities
{
    [Table("saving_monitoring")]
    public class SavingMonitoring
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("idea_id")]
        public int IdeaId { get; set; }
        [ForeignKey("IdeaId")]
        public Idea Idea { get; set; } = null!;

        [Required]
        [Column("monitoring_period", TypeName = "varchar(20)")]
        public string MonitoringPeriod { get; set; } = string.Empty; // Monthly, Quarterly, Yearly

        [Required]
        [Column("period_start_date")]
        public DateTime PeriodStartDate { get; set; }

        [Required]
        [Column("period_end_date")]
        public DateTime PeriodEndDate { get; set; }

        [Required]
        [Column("planned_saving", TypeName = "decimal(18,2)")]
        public decimal PlannedSaving { get; set; }

        [Column("actual_saving", TypeName = "decimal(18,2)")]
        public decimal? ActualSaving { get; set; }

        [Column("variance", TypeName = "decimal(18,2)")]
        public decimal? Variance { get; set; }

        [Column("variance_percentage", TypeName = "decimal(5,2)")]
        public decimal? VariancePercentage { get; set; }

        [Column("variance_reason", TypeName = "nvarchar(500)")]
        public string? VarianceReason { get; set; }

        [Column("supporting_documents", TypeName = "nvarchar(max)")]
        public string? SupportingDocuments { get; set; }

        [Column("monitoring_notes", TypeName = "nvarchar(1000)")]
        public string? MonitoringNotes { get; set; }

        [Required]
        [Column("reported_by", TypeName = "varchar(10)")]
        public string ReportedBy { get; set; } = string.Empty;
        [ForeignKey("ReportedBy")]
        public Employee Reporter { get; set; } = null!;

        [Column("reviewed_by", TypeName = "varchar(10)")]
        public string? ReviewedBy { get; set; }
        [ForeignKey("ReviewedBy")]
        public Employee? Reviewer { get; set; }

        [Column("review_status", TypeName = "varchar(20)")]
        public string ReviewStatus { get; set; } = "Pending"; // Pending, Approved, Questioned

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("updated_date")]
        public DateTime? UpdatedDate { get; set; }
    }
}