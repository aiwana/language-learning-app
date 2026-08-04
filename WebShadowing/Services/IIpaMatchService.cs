using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed record GetIpaMatchQuestionCommand(
    long LessonId,
    long SentenceId);

public sealed record SubmitIpaMatchAnswerCommand(
    string QuestionToken,
    string OptionId);

public interface IIpaMatchService
{
    Task<IpaMatchQuestionDto> GetQuestionAsync(
        GetIpaMatchQuestionCommand command,
        CancellationToken cancellationToken = default);

    Task<PracticeAnswerEvaluationDto> SubmitAnswerAsync(
        SubmitIpaMatchAnswerCommand command,
        CancellationToken cancellationToken = default);
}