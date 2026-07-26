namespace WebShadowing.Services;

public interface IPronunciationAssessmentProvider
{
    string ProviderName { get; }

    Task<PronunciationAssessmentResult> AssessAsync(
        PronunciationAssessmentRequest request,
        CancellationToken cancellationToken = default);
}
