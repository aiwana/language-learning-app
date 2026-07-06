using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Courses")]
public class Course
{
    [Key]
    [Column("course_id")]
    public long CourseId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string Level { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("learning_mode")]
    public string LearningMode { get; set; } = LearningModes.Casual;

    [Required]
    [MaxLength(20)]
    [Column("course_type")]
    public string CourseType { get; set; } = CourseTypes.Curriculum;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
