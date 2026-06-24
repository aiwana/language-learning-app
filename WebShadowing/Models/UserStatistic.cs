using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("User_Statistics")]
public class UserStatistic
{
    [Key]
    [Column("stat_id")]
    public long StatId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("total_sessions")]
    public int TotalSessions { get; set; }

    [Column("average_score", TypeName = "decimal(5,2)")]
    public decimal AverageScore { get; set; }

    [Column("streak_days")]
    public int StreakDays { get; set; }

    [Column("last_practice_at")]
    public DateTime? LastPracticeAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
