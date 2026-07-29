using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Lesson_Sentences")]
public class LessonSentence
{
    [Key]
    [Column("sentence_id")]
    public long SentenceId { get; set; }

    [Column("lesson_id")]
    public long LessonId { get; set; }

    [Column("sentence_order")]
    public int SentenceOrder { get; set; }

    [Required]
    [Column("text")]
    public string Text { get; set; } = string.Empty;

    public string? Translation { get; set; }

    [MaxLength(500)]
    [Column("ipa")]
    public string? Ipa { get; set; }

    [Column("start_ms")]
    public int? StartMilliseconds { get; set; }

    [Column("end_ms")]
    public int? EndMilliseconds { get; set; }

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}
