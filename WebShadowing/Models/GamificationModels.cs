using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

public sealed class GamificationOptions
{
    public const string SectionName = "Gamification";

    [Range(0, int.MaxValue)]
    public int SentenceCompletionExp { get; set; } = 20;

    [Range(0, int.MaxValue)]
    public int FailedAttemptHeartCost { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int HeartExchangeExpCost { get; set; } = 100;

    [Range(1, int.MaxValue)]
    public int HeartExchangeAmount { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int MaxHearts { get; set; } = 5;

    [Required]
    public string BusinessTimeZone { get; set; } = "Asia/Ho_Chi_Minh";
}

public static class GamificationSourceTypes
{
    public const string SentenceCompletion = "sentence_completion";
    public const string AttemptPenalty = "attempt_penalty";
    public const string DailyActivity = "daily_activity";
    public const string HeartExchange = "heart_exchange";
}

[Table("Gamification_Ledger")]
public sealed class GamificationLedgerEntry
{
    [Key, Column("ledger_id")]
    public long LedgerId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("attempt_id")]
    public long? AttemptId { get; set; }

    [Required, MaxLength(30), Column("source_type")]
    public string SourceType { get; set; } = string.Empty;

    [Required, MaxLength(200), Column("source_id")]
    public string SourceId { get; set; } = string.Empty;

    [Required, MaxLength(100), Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("exp_delta")]
    public int ExpDelta { get; set; }

    [Column("hearts_delta")]
    public int HeartsDelta { get; set; }

    [Column("streak_delta")]
    public int StreakDelta { get; set; }

    [Column("exp_balance")]
    public int ExpBalance { get; set; }

    [Column("hearts_balance")]
    public int HeartsBalance { get; set; }

    [Column("streak_balance")]
    public int StreakBalance { get; set; }

    [Column("is_vip")]
    public bool IsVip { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public PracticeAttempt? Attempt { get; set; }
}

public sealed class GamificationBalanceDto
{
    public int Exp { get; init; }
    public int Hearts { get; init; }
    public int StreakDays { get; init; }
    public bool IsVip { get; init; }
    public bool HasInfiniteHearts => IsVip;
}

public sealed class GamificationDeltaDto
{
    public int Exp { get; init; }
    public int Hearts { get; init; }
    public int StreakDays { get; init; }
}

public sealed class GamificationTransactionDto
{
    public bool Succeeded { get; init; }
    public bool Applied { get; init; }
    public bool AlreadyProcessed { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string? RejectionCode { get; init; }
    public string? Message { get; init; }
    public GamificationDeltaDto Delta { get; init; } = new();
    public GamificationBalanceDto Balance { get; init; } = new();
}

public sealed class VerifiedPracticeAttempt
{
    public long UserId { get; init; }
    public long LessonId { get; init; }
    public long SentenceId { get; init; }
    public string PracticeTab { get; init; } = PracticeTabs.Shadowing;
    public string ExerciseType { get; init; } = ExerciseTypes.Pronunciation;
    public decimal TargetScore { get; init; }
    public decimal Score { get; init; }
    public bool Passed { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? AssessmentProvider { get; init; }
    public string? ProviderReferenceId { get; init; }
    public string? TranscriptText { get; init; }
    public string? FeedbackText { get; init; }
    public IReadOnlyList<VerifiedPracticeWord> Words { get; init; } = [];
}

public sealed record VerifiedPracticeWord(
    string Word,
    string AccuracyCode);

public sealed class HeartExchangeRequestDto
{
    [Required, StringLength(100, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;
}
