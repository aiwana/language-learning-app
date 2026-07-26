using Microsoft.Extensions.Options;

namespace WebShadowing.Services;

public sealed class HybridPronunciationAssessmentService : IPronunciationAssessmentService
{
    private readonly AzurePronunciationAssessmentService _primaryProvider;
    private readonly OpenAiPronunciationAssessmentService _fallbackProvider;
    private readonly PronunciationAssessmentOptions _options;
    private readonly ILogger<HybridPronunciationAssessmentService> _logger;

    public HybridPronunciationAssessmentService(
        AzurePronunciationAssessmentService primaryProvider,
        OpenAiPronunciationAssessmentService fallbackProvider,
        IOptions<PronunciationAssessmentOptions> options,
        ILogger<HybridPronunciationAssessmentService> logger)
    {
        _primaryProvider = primaryProvider;
        _fallbackProvider = fallbackProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PronunciationAssessmentResult> AssessAsync(
        PronunciationAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryProvider.AssessAsync(request, cancellationToken);
        }
        catch (PronunciationAssessmentUnavailableException exception) when (_options.EnableOpenAiFallback)
        {
            _logger.LogWarning(
                "Primary pronunciation provider failed with {ErrorCode}. Falling back to OpenAI.",
                exception.ErrorCode);
            return await _fallbackProvider.AssessAsync(request, cancellationToken);
        }
    }
}
