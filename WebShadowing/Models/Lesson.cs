using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Lessons")]
public class Lesson
{
    [Key]
    [Column("lesson_id")]
    public long LessonId { get; set; }

    [Column("course_id")]
    public long CourseId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column("lesson_order")]
    public int LessonOrder { get; set; }

    public int Duration { get; set; }

    [Column("created_by_user_id")]
    public long? CreatedByUserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Source { get; set; } = LessonSources.Curated;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public ICollection<LessonMaterial> Materials { get; set; } = new List<LessonMaterial>();

    public ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}
