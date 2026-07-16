using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed record PronunciationAssessmentRequest(
    byte[] Audio,
    string AudioFormat,
    string TargetText,
    string? TargetIpa,
    byte PronunciationTarget);

public sealed class PronunciationAssessmentUnavailableException : Exception
{
    public PronunciationAssessmentUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public interface IPronunciationAssessmentService
{
    Task<ShadowingEvaluationDto> AssessAsync(
        PronunciationAssessmentRequest request,
        CancellationToken cancellationToken = default);
}
