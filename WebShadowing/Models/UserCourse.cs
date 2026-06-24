using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Users_Courses")]
public class UserCourse
{
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("course_id")]
    public long CourseId { get; set; }

    [Column("enrolled_at")]
    public DateTime EnrolledAt { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Progress { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;
}
