using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed record PronunciationAssessmentRequest(
    byte[] Audio,
    string AudioFormat,
    string Accent,
    string LearningMode,
    string TargetText,
    string? TargetIpa,
    byte PronunciationTarget);

public sealed class PronunciationAssessmentUnavailableException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public PronunciationAssessmentUnavailableException(
        string message,
        int statusCode = StatusCodes.Status503ServiceUnavailable,
        string errorCode = "pronunciation_assessment_unavailable",
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public sealed class PronunciationAssessmentResult
{
    public string Provider { get; init; } = "unknown";
    public string? ProviderReferenceId { get; init; }
    public int OverallScore { get; init; }
    public int? AccuracyScore { get; init; }
    public int? FluencyScore { get; init; }
    public int? CompletenessScore { get; init; }
    public int? ProsodyScore { get; init; }
    public string Transcript { get; init; } = string.Empty;
    public string Feedback { get; init; } = string.Empty;
    public IReadOnlyList<PronunciationWordResult> Words { get; init; } = [];
}

public sealed class PronunciationWordResult
{
    public string Word { get; init; } = string.Empty;
    public string AccuracyCode { get; init; } = "warning";
    public string? Correction { get; init; }
    public IReadOnlyList<PronunciationPhonemeResult> Phonemes { get; init; } = [];
}

public sealed class PronunciationPhonemeResult
{
    public string Symbol { get; init; } = string.Empty;
    public string AccuracyCode { get; init; } = "warning";
}

public interface IPronunciationAssessmentService
{
    Task<PronunciationAssessmentResult> AssessAsync(
        PronunciationAssessmentRequest request,
        CancellationToken cancellationToken = default);
}
