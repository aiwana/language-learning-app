using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Admin_Audit_Log")]
public sealed class AdminAuditLog
{
    [Key, Column("audit_id")]
    public long AuditId { get; set; }

    [Column("actor_user_id")]
    public long ActorUserId { get; set; }

    [Column("target_user_id")]
    public long TargetUserId { get; set; }

    [Required, MaxLength(40), Column("action")]
    public string Action { get; set; } = string.Empty;

    [MaxLength(1000), Column("detail")]
    public string? Detail { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Actor { get; set; } = null!;
    public User Target { get; set; } = null!;
}

public sealed record AdminUserListItemDto(
    long UserId,
    string FullName,
    string Email,
    string Username,
    string Role,
    string LearningMode,
    bool IsVip,
    bool IsActive,
    DateTime? LastPracticeAt,
    IReadOnlyList<string> RecentPracticeTabs);

public sealed record AdminUserUsageDto(
    DateTime? LastPracticeAt,
    int Hearts,
    int Exp,
    int StreakDays,
    int TotalSessions,
    decimal AverageScore,
    IReadOnlyDictionary<string, int> PracticeTabCounts30d,
    int AiDialogueSessions30d,
    int SavedAiLessons30d);

public sealed record AdminAuditItemDto(
    long AuditId,
    long ActorUserId,
    string ActorEmail,
    string Action,
    string? Detail,
    DateTime CreatedAt);

public sealed record AdminUserDetailDto(
    long UserId,
    string FullName,
    string Email,
    string Username,
    string Role,
    string LearningMode,
    byte PronunciationTarget,
    string Accent,
    bool IsVip,
    bool IsActive,
    bool OnboardingCompleted,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DisabledAt,
    string? DisabledReason,
    long? DisabledByUserId,
    SubscriptionDto? CurrentSubscription,
    AdminUserUsageDto Usage,
    IReadOnlyList<AdminAuditItemDto> RecentAudits);

public sealed record AdminUserSearchResult(
    IReadOnlyList<AdminUserListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminActionResult(bool Succeeded, string Message)
{
    public static AdminActionResult Ok(string message = "Thành công.") => new(true, message);
    public static AdminActionResult Fail(string message) => new(false, message);
}
