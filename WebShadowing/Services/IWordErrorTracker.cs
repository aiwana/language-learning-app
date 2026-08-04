using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IWordErrorTracker
{
    Task TrackAsync(long userId, LessonSentenceDto sentence, IReadOnlyList<WordFeedbackDto> feedback, CancellationToken cancellationToken = default);
}
