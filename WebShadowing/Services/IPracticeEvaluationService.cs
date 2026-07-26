namespace WebShadowing.Services;

public sealed record EvaluateShadowingCommand(
    long LessonId,
    long SentenceId,
    int SentenceIndex,
    byte[] Audio,
    string AudioFormat,
    string ContentType,
    string IdempotencyKey);

public interface IPracticeEvaluationService
{
    Task<WebShadowing.Models.ShadowingEvaluationDto> EvaluateAsync(
        EvaluateShadowingCommand command,
        CancellationToken cancellationToken = default);
}
