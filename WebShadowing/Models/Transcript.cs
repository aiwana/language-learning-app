using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebShadowing.Models;

[Table("Transcripts")]
public class Transcript
{
    [Key]
    [Column("transcript_id")]
    public long TranscriptId { get; set; }

    [Column("recording_id")]
    public long RecordingId { get; set; }

    [Required]
    [Column("transcript_text")]
    public string TranscriptText { get; set; } = string.Empty;

    [Column("confidence_score", TypeName = "decimal(5,2)")]
    public decimal? ConfidenceScore { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(RecordingId))]
    public UserRecording Recording { get; set; } = null!;
}
