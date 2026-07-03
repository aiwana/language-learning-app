using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("User_Recordings")]
public class UserRecording
{
    [Key]
    [Column("recording_id")]
    public long RecordingId { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Required]
    [Column("audio_url")]
    public string AudioUrl { get; set; } = string.Empty;

    public int Duration { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(SessionId))]
    public PracticeSession Session { get; set; } = null!;

    public ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();
}
