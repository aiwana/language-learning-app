using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Users")]
public class User
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("learning_mode")]
    public string LearningMode { get; set; } = LearningModes.Casual;

    [Column("pronunciation_target")]
    public byte PronunciationTarget { get; set; } = PronunciationTargets.Comprehension70;

    [Required]
    [MaxLength(10)]
    public string Accent { get; set; } = Accents.EnUs;

    [Column("is_vip")]
    public bool IsVip { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();

    public ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();

    public UserStatistic? Statistics { get; set; }
}
