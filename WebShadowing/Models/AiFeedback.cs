using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("AI_Feedback")]
public class AiFeedback
{
    [Key]
    [Column("feedback_id")]
    public long FeedbackId { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("pronunciation_score", TypeName = "decimal(5,2)")]
    public decimal? PronunciationScore { get; set; }

    [Column("fluency_score", TypeName = "decimal(5,2)")]
    public decimal? FluencyScore { get; set; }

    [Column("accuracy_score", TypeName = "decimal(5,2)")]
    public decimal? AccuracyScore { get; set; }

    [Column("feedback_text")]
    public string? FeedbackText { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(SessionId))]
    public PracticeSession Session { get; set; } = null!;
}
