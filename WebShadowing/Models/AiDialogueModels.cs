using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

public sealed class AiDialogueOptions
{
    public const string SectionName = "AiDialogue";
    [Required] public string Model { get; set; } = "gpt-4o-mini";
    [Required] public string TranscriptionModel { get; set; } = "gpt-4o-mini-transcribe";
    [Required] public string TtsModel { get; set; } = "tts-1-hd";
    [Required] public string TtsVoiceUs { get; set; } = "nova";
    [Required] public string TtsVoiceGb { get; set; } = "fable";
    [Range(1, 100)] public int MaxTurnsPerSession { get; set; } = 30;
    [Range(1, 1440)] public int SessionTimeoutMinutes { get; set; } = 15;
    [Required] public string AudioLocalPath { get; set; } = "wwwroot/media/generated";
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

public sealed class StartDialogueRequestDto { public long? LessonId { get; set; } }
public sealed class DialogueMessageRequestDto { [Required, StringLength(2000, MinimumLength = 1)] public string Message { get; set; } = string.Empty; }
public sealed record DialogueTurnDto(string Role, string Text, string? AudioUrl, DateTime CreatedAt);
public sealed record DialogueSessionDto(long SessionId, string Status, int TurnCount, int MaxTurns, IReadOnlyList<DialogueTurnDto> Turns);
public sealed record DialogueReplyDto(long SessionId, int TurnCount, string UserText, string ReplyText, string? AudioUrl, bool Completed);
