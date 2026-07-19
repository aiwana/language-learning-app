using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("User_Lesson_Progress")]
public sealed class UserLessonProgress
{
    [Key, Column("progress_id")]
    public long ProgressId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("lesson_id")]
    public long LessonId { get; set; }

    [Required, MaxLength(20), Column("practice_tab")]
    public string PracticeTab { get; set; } = PracticeTabs.Shadowing;

    [Column("current_sentence_id")]
    public long? CurrentSentenceId { get; set; }

    [Required, MaxLength(20), Column("status")]
    public string Status { get; set; } = ProgressStatuses.NotStarted;

    [Column("completed_sentence_count")]
    public int CompletedSentenceCount { get; set; }

    [Column("progress_percent", TypeName = "decimal(5,2)")]
    public decimal ProgressPercent { get; set; }

    [Column("last_position_ms")]
    public int? LastPositionMilliseconds { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp, Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User User { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
    public LessonSentence? CurrentSentence { get; set; }
}

[Table("User_Sentence_Progress")]
public sealed class UserSentenceProgress
{
    [Key, Column("sentence_progress_id")]
    public long SentenceProgressId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("sentence_id")]
    public long SentenceId { get; set; }

    [Required, MaxLength(20), Column("practice_tab")]
    public string PracticeTab { get; set; } = PracticeTabs.Shadowing;

    [Required, MaxLength(20), Column("status")]
    public string Status { get; set; } = ProgressStatuses.NotStarted;

    [Column("best_score", TypeName = "decimal(5,2)")]
    public decimal? BestScore { get; set; }

    [Column("attempt_count")]
    public int AttemptCount { get; set; }

    [Column("last_attempt_at")]
    public DateTime? LastAttemptAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp, Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User User { get; set; } = null!;
    public LessonSentence Sentence { get; set; } = null!;
}

[Table("Practice_Attempts")]
public sealed class PracticeAttempt
{
    [Key, Column("attempt_id")]
    public long AttemptId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("session_id")]
    public long? SessionId { get; set; }

    [Column("sentence_id")]
    public long? SentenceId { get; set; }

    [Column("saved_segment_id")]
    public long? SavedSegmentId { get; set; }

    [Required, MaxLength(20), Column("practice_tab")]
    public string PracticeTab { get; set; } = PracticeTabs.Shadowing;

    [Required, MaxLength(30), Column("exercise_type")]
    public string ExerciseType { get; set; } = ExerciseTypes.Pronunciation;

    [Column("target_score", TypeName = "decimal(5,2)")]
    public decimal TargetScore { get; set; }

    [Column("score", TypeName = "decimal(5,2)")]
    public decimal? Score { get; set; }

    [Required, MaxLength(20), Column("result")]
    public string Result { get; set; } = AttemptResults.Pending;

    [MaxLength(100), Column("assessment_provider")]
    public string? AssessmentProvider { get; set; }

    [MaxLength(255), Column("provider_reference_id")]
    public string? ProviderReferenceId { get; set; }

    [Column("transcript_text")]
    public string? TranscriptText { get; set; }

    [Column("feedback_text")]
    public string? FeedbackText { get; set; }

    [Required, MaxLength(100), Column("idempotency_key")]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Column("attempted_at")]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public PracticeSession? Session { get; set; }
    public LessonSentence? Sentence { get; set; }
    public SavedAiLessonSegment? SavedSegment { get; set; }
}

[Table("Word_Error_Statistics")]
public sealed class WordErrorStatistic
{
    [Key, Column("word_error_stat_id")]
    public long WordErrorStatisticId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Required, MaxLength(100), Column("normalized_word")]
    public string NormalizedWord { get; set; } = string.Empty;

    [Required, MaxLength(100), Column("display_word")]
    public string DisplayWord { get; set; } = string.Empty;

    [Column("consecutive_error_count")]
    public int ConsecutiveErrorCount { get; set; }

    [Column("total_error_count")]
    public int TotalErrorCount { get; set; }

    [Column("last_error_at")]
    public DateTime? LastErrorAt { get; set; }

    [Column("last_attempted_at")]
    public DateTime? LastAttemptedAt { get; set; }

    [Column("last_sentence_id")]
    public long? LastSentenceId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp, Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User User { get; set; } = null!;
    public LessonSentence? LastSentence { get; set; }
}

[Table("Vocabulary_Items")]
public sealed class VocabularyItem
{
    [Key, Column("vocabulary_item_id")]
    public long VocabularyItemId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Required, MaxLength(100), Column("normalized_word")]
    public string NormalizedWord { get; set; } = string.Empty;

    [Required, MaxLength(100), Column("display_word")]
    public string DisplayWord { get; set; } = string.Empty;

    [Required, MaxLength(10), Column("language_code")]
    public string LanguageCode { get; set; } = "en";

    [MaxLength(100), Column("ipa")]
    public string? Ipa { get; set; }

    [Column("meaning")]
    public string? Meaning { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("example_sentence")]
    public string? ExampleSentence { get; set; }

    [Column("source_sentence_id")]
    public long? SourceSentenceId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp, Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User User { get; set; } = null!;
    public LessonSentence? SourceSentence { get; set; }
}

[Table("Favorite_Sentences")]
public sealed class FavoriteSentence
{
    [Key, Column("favorite_sentence_id")]
    public long FavoriteSentenceId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("sentence_id")]
    public long SentenceId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public LessonSentence Sentence { get; set; } = null!;
}

[Table("User_Settings")]
public sealed class UserSettings
{
    [Key, Column("user_settings_id")]
    public long UserSettingsId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("auto_save_ai_lessons")]
    public bool AutoSaveAiLessons { get; set; }

    [Column("show_translation")]
    public bool ShowTranslation { get; set; } = true;

    [Column("show_captions")]
    public bool ShowCaptions { get; set; } = true;

    [Required, MaxLength(10), Column("theme")]
    public string Theme { get; set; } = ThemePreferences.System;

    [Column("playback_rate", TypeName = "decimal(3,2)")]
    public decimal PlaybackRate { get; set; } = 1m;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp, Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User User { get; set; } = null!;
}

[Table("Mode_Change_History")]
public sealed class ModeChangeHistory
{
    [Key, Column("mode_change_id")]
    public long ModeChangeId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Required, MaxLength(20), Column("from_mode")]
    public string FromMode { get; set; } = LearningModes.Casual;

    [Required, MaxLength(20), Column("to_mode")]
    public string ToMode { get; set; } = LearningModes.Casual;

    [Required, MaxLength(20), Column("changed_by")]
    public string ChangedBy { get; set; } = ModeChangeActors.User;

    [MaxLength(500), Column("reason")]
    public string? Reason { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

[Table("User_Saved_Lessons")]
public sealed class SavedAiLesson
{
    [Key, Column("saved_lesson_id")]
    public long SavedLessonId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Required, MaxLength(255), Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(20), Column("learning_mode")]
    public string LearningMode { get; set; } = LearningModes.Casual;

    [Required, Column("content")]
    public string ContentSnapshot { get; set; } = string.Empty;

    [MaxLength(100), Column("source_provider")]
    public string? SourceProvider { get; set; }

    [MaxLength(255), Column("source_id")]
    public string? SourceId { get; set; }

    [Column("media_url")]
    public string? MediaUrl { get; set; }

    [MaxLength(1000), Column("license_note")]
    public string? LicenseNote { get; set; }

    [Required, MaxLength(20), Column("source_review_status")]
    public string SourceReviewStatus { get; set; } = SourceReviewStatuses.Pending;

    [Column("source_reviewed_at")]
    public DateTime? SourceReviewedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp, Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User User { get; set; } = null!;
    public ICollection<SavedAiLessonSegment> Segments { get; set; } = new List<SavedAiLessonSegment>();
}

[Table("Saved_AI_Lesson_Segments")]
public sealed class SavedAiLessonSegment
{
    [Key, Column("saved_segment_id")]
    public long SavedSegmentId { get; set; }

    [Column("saved_lesson_id")]
    public long SavedLessonId { get; set; }

    [Column("segment_order")]
    public int SegmentOrder { get; set; }

    [Required, Column("text")]
    public string Text { get; set; } = string.Empty;

    [Column("translation")]
    public string? Translation { get; set; }

    [MaxLength(100), Column("speaker")]
    public string? Speaker { get; set; }

    [Column("start_ms")]
    public int? StartMilliseconds { get; set; }

    [Column("end_ms")]
    public int? EndMilliseconds { get; set; }

    public SavedAiLesson SavedLesson { get; set; } = null!;
}

[Table("VIP_Subscriptions")]
public sealed class VipSubscription
{
    [Key, Column("subscription_id")]
    public long SubscriptionId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Required, MaxLength(50), Column("plan_code")]
    public string PlanCode { get; set; } = string.Empty;

    [Required, MaxLength(20), Column("billing_period")]
    public string BillingPeriod { get; set; } = BillingPeriods.Monthly;

    [Required, MaxLength(20), Column("status")]
    public string Status { get; set; } = SubscriptionStatuses.Pending;

    [Required, MaxLength(100), Column("provider")]
    public string Provider { get; set; } = "internal";

    [Required, MaxLength(255), Column("provider_subscription_id")]
    public string ProviderSubscriptionId { get; set; } = string.Empty;

    [Column("starts_at")]
    public DateTime StartsAt { get; set; }

    [Column("ends_at")]
    public DateTime? EndsAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("auto_renew")]
    public bool AutoRenew { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp, Column("row_version")]
    public byte[]? RowVersion { get; set; }

    public User User { get; set; } = null!;
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}

[Table("Payment_Transactions")]
public sealed class PaymentTransaction
{
    [Key, Column("payment_transaction_id")]
    public long PaymentTransactionId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("subscription_id")]
    public long? SubscriptionId { get; set; }

    [Required, MaxLength(100), Column("provider")]
    public string Provider { get; set; } = "internal";

    [Required, MaxLength(255), Column("provider_transaction_id")]
    public string ProviderTransactionId { get; set; } = string.Empty;

    [Required, MaxLength(100), Column("idempotency_key")]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required, MaxLength(20), Column("transaction_type")]
    public string TransactionType { get; set; } = PaymentTypes.Purchase;

    [Required, MaxLength(20), Column("status")]
    public string Status { get; set; } = PaymentStatuses.Pending;

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3), Column("currency")]
    public string Currency { get; set; } = "VND";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    public User User { get; set; } = null!;
    public VipSubscription? Subscription { get; set; }
}
