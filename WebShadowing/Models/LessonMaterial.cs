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

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}
