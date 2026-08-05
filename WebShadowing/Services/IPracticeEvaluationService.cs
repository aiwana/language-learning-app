namespace WebShadowing.Services;

public sealed record EvaluateShadowingCommand(
    long LessonId,
    long SentenceId,
    int SentenceIndex,
    byte[] Audio,
    string AudioFormat,
    string ContentType,
    string IdempotencyKey,
    long? SavedSegmentId = null,
    Guid? PreviewId = null);

public sealed record EvaluatePracticeAnswerCommand(
    long LessonId,
    long SentenceId,
    string PracticeTab,
    string Answer,
    string IdempotencyKey,
    int? TargetIndex = null);

public interface IPracticeEvaluationService
{
    Task<WebShadowing.Models.ShadowingEvaluationDto> EvaluateAsync(
        EvaluateShadowingCommand command,
        CancellationToken cancellationToken = default);

    Task<WebShadowing.Models.PracticeAnswerEvaluationDto> EvaluateAnswerAsync(
        EvaluatePracticeAnswerCommand command,
        CancellationToken cancellationToken = default);
}
