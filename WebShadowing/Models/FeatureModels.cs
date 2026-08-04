using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

public sealed class VocabularyOptions
{
    public const string SectionName = "Vocabulary";
    [Range(1, 20)] public int WordErrorThreshold { get; set; } = 3;
    [Range(1, 100)] public int DefaultPageSize { get; set; } = 24;
}

public sealed class ModeChangeOptions
{
    public const string SectionName = "ModeChange";
    [Range(0, 31)] public int FreeChangesPerMonth { get; set; } = 1;
    [Range(0, int.MaxValue)] public int ExpCostPerChange { get; set; } = 200;
    public bool VipUnlimited { get; set; } = true;
}

public sealed class AiLessonOptions
{
    public const string SectionName = "AiLesson";
    [Range(3, 30)] public int MaxSentencesPerLesson { get; set; } = 12;
    [Range(1, 10080)] public int PreviewLifetimeMinutes { get; set; } = 1440;
    public string GenerationModel { get; set; } = "gpt-4o";
    public string TtsModel { get; set; } = "tts-1-hd";
    public string TtsVoiceUs { get; set; } = "nova";
    public string TtsVoiceGb { get; set; } = "fable";
}

public sealed class AiDialogueOptions
{
    public const string SectionName = "AiDialogue";
    public string Model { get; set; } = "gpt-4o-mini";
    public string TranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";
    [Range(2, 100)] public int MaxTurnsPerSession { get; set; } = 30;
    [Range(1, 120)] public int SessionTimeoutMinutes { get; set; } = 15;
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "local";
    public string LocalPath { get; set; } = "wwwroot/media/generated";
}

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";
    [Range(0, double.MaxValue)] public decimal VipMonthlyPrice { get; set; } = 99_000;
    [Range(0, double.MaxValue)] public decimal VipYearlyPrice { get; set; } = 799_000;
    public MomoOptions Momo { get; set; } = new();
    public ZaloPayOptions ZaloPay { get; set; } = new();
}

public sealed class MomoOptions
{
    public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
}

public sealed class ZaloPayOptions
{
    public string Endpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/create";
    public string AppId { get; set; } = string.Empty;
    public string Key1 { get; set; } = string.Empty;
    public string Key2 { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}

public sealed record VocabularyItemDto(
    long VocabularyItemId,
    string Word,
    string? Ipa,
    string? Meaning,
    string? ExampleSentence,
    string ReviewStatus,
    DateTime? LastReviewedAt,
    int ReviewCount,
    long? SourceSentenceId);

public sealed record VocabularyPageDto(IReadOnlyList<VocabularyItemDto> Items, int Total, int Page, int PageSize);

public sealed class AddVocabularyRequestDto
{
    [Required, StringLength(100)] public string Word { get; set; } = string.Empty;
    [StringLength(100)] public string? Ipa { get; set; }
    [StringLength(4000)] public string? Meaning { get; set; }
    [StringLength(4000)] public string? ExampleSentence { get; set; }
    public long? SourceSentenceId { get; set; }
}

public sealed record FavoriteSentenceDto(
    long FavoriteSentenceId,
    long SentenceId,
    long LessonId,
    string LessonTitle,
    string Text,
    string? Translation,
    DateTime CreatedAt);

public sealed class AddFavoriteRequestDto
{
    [Range(1, long.MaxValue)] public long SentenceId { get; set; }
}

public sealed record UserProfileDto(
    long UserId,
    string FullName,
    string Email,
    string? Phone,
    string LearningMode,
    byte PronunciationTarget,
    string Accent,
    bool IsVip,
    bool AutoSaveAiLessons,
    string Theme,
    int Exp,
    int FreeModeChangesRemaining,
    int ModeChangeExpCost);

public sealed class UpdateProfileRequestDto
{
    [Required, StringLength(255, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [Phone, StringLength(20)] public string? Phone { get; set; }
}

public sealed class UpdateLearningSettingsRequestDto
{
    [Range(50, 90)] public byte PronunciationTarget { get; set; }
    [Required, StringLength(10)] public string Accent { get; set; } = Accents.EnUs;
    public bool AutoSaveAiLessons { get; set; }
    [Required, StringLength(10)] public string Theme { get; set; } = ThemePreferences.System;
}

public sealed class ChangeLearningModeRequestDto
{
    [Required, StringLength(20)] public string LearningMode { get; set; } = LearningModes.Casual;
    public bool UseExpIfNeeded { get; set; }
}

public sealed record ModeChangeResultDto(
    bool Succeeded,
    string LearningMode,
    bool UsedFreeChange,
    int ExpCharged,
    int ExpBalance,
    int FreeChangesRemaining,
    string? Message = null);

public sealed record AiLessonSegmentDto(
    int Order,
    string Text,
    string Translation,
    string Ipa,
    string? AudioUrl,
    string? Speaker = null,
    long? SavedSegmentId = null);

public sealed record AiLessonPreviewDto(
    Guid PreviewId,
    string Title,
    string LearningMode,
    string Accent,
    IReadOnlyList<AiLessonSegmentDto> Segments,
    DateTime ExpiresAt,
    bool Saved = false,
    long? SavedLessonId = null);

public sealed class GenerateAiLessonRequestDto
{
    [Required, StringLength(1000, MinimumLength = 5)] public string Prompt { get; set; } = string.Empty;
    [Range(3, 20)] public int SentenceCount { get; set; } = 8;
}

public sealed class SaveAiLessonRequestDto
{
    public Guid PreviewId { get; set; }
}

public sealed record SavedAiLessonDto(
    long SavedLessonId,
    string Title,
    string LearningMode,
    DateTime UpdatedAt,
    IReadOnlyList<AiLessonSegmentDto> Segments);

[Table("AI_Lesson_Previews")]
public sealed class AiLessonPreview
{
    [Key, Column("preview_id")] public Guid PreviewId { get; set; }
    [Column("user_id")] public long UserId { get; set; }
    [Required, MaxLength(1000), Column("prompt")] public string Prompt { get; set; } = string.Empty;
    [Required, MaxLength(255), Column("title")] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(20), Column("learning_mode")] public string LearningMode { get; set; } = LearningModes.Casual;
    [Required, MaxLength(10), Column("accent")] public string Accent { get; set; } = Accents.EnUs;
    [Required, Column("content_json")] public string ContentJson { get; set; } = string.Empty;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("expires_at")] public DateTime ExpiresAt { get; set; }
    [Column("saved_lesson_id")] public long? SavedLessonId { get; set; }
    public User User { get; set; } = null!;
    public SavedAiLesson? SavedLesson { get; set; }
}

[Table("AI_Dialogue_Sessions")]
public sealed class AiDialogueSession
{
    [Key, Column("dialogue_session_id")] public long DialogueSessionId { get; set; }
    [Column("user_id")] public long UserId { get; set; }
    [Column("lesson_id")] public long? LessonId { get; set; }
    [Required, MaxLength(20), Column("learning_mode")] public string LearningMode { get; set; } = LearningModes.Casual;
    [Required, MaxLength(20), Column("status")] public string Status { get; set; } = "active";
    [Column("turn_count")] public int TurnCount { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("last_activity_at")] public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    [Column("ended_at")] public DateTime? EndedAt { get; set; }
    public User User { get; set; } = null!;
    public Lesson? Lesson { get; set; }
    public ICollection<AiDialogueTurn> Turns { get; set; } = new List<AiDialogueTurn>();
}

[Table("AI_Dialogue_Turns")]
public sealed class AiDialogueTurn
{
    [Key, Column("dialogue_turn_id")] public long DialogueTurnId { get; set; }
    [Column("dialogue_session_id")] public long DialogueSessionId { get; set; }
    [Required, MaxLength(20), Column("role")] public string Role { get; set; } = "user";
    [Required, Column("text")] public string Text { get; set; } = string.Empty;
    [Column("audio_url")] public string? AudioUrl { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AiDialogueSession Session { get; set; } = null!;
}

public sealed class StartDialogueRequestDto
{
    public long? LessonId { get; set; }
}

public sealed class DialogueMessageRequestDto
{
    [Required, StringLength(2000, MinimumLength = 1)] public string Message { get; set; } = string.Empty;
}

public sealed record DialogueTurnDto(string Role, string Text, string? AudioUrl, DateTime CreatedAt);
public sealed record DialogueSessionDto(long SessionId, string Status, int TurnCount, int MaxTurns, IReadOnlyList<DialogueTurnDto> Turns);
public sealed record DialogueReplyDto(long SessionId, int TurnCount, string UserText, string ReplyText, string? AudioUrl, bool Completed);

public sealed class CheckoutRequestDto
{
    [Required, StringLength(20)] public string Provider { get; set; } = PaymentProviders.Momo;
    [Required, StringLength(20)] public string BillingPeriod { get; set; } = BillingPeriods.Monthly;
    [Required, StringLength(100, MinimumLength = 8)] public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed record CheckoutResultDto(bool Succeeded, string? PayUrl, long? TransactionId, string? Message);
public sealed record SubscriptionDto(long SubscriptionId, string PlanCode, string BillingPeriod, string Status, string Provider, DateTime StartsAt, DateTime? EndsAt, bool AutoRenew);
