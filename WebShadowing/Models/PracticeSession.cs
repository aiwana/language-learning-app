using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Practice_Sessions")]
public class PracticeSession
{
    [Key]
    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("lesson_id")]
    public long LessonId { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("overall_score", TypeName = "decimal(5,2)")]
    public decimal? OverallScore { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;

    public ICollection<UserRecording> Recordings { get; set; } = new List<UserRecording>();

    public ICollection<AiFeedback> AiFeedbacks { get; set; } = new List<AiFeedback>();
}
