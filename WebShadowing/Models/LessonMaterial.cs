using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Lesson_Material")]
public class LessonMaterial
{
    [Key]
    [Column("material_id")]
    public long MaterialId { get; set; }

    [Column("lesson_id")]
    public long LessonId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("material_type")]
    public string MaterialType { get; set; } = string.Empty;

    [Required]
    [Column("content_url")]
    public string ContentUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("source_provider")]
    public string? SourceProvider { get; set; }

    [MaxLength(255)]
    [Column("source_id")]
    public string? SourceId { get; set; }

    [MaxLength(1000)]
    [Column("license_note")]
    public string? LicenseNote { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("source_review_status")]
    public string SourceReviewStatus { get; set; } = SourceReviewStatuses.Pending;

    [Column("source_reviewed_at")]
    public DateTime? SourceReviewedAt { get; set; }

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}
