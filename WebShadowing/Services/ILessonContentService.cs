using WebShadowing.Models;

namespace WebShadowing.Services;

public interface ILessonContentService
{
    Task<IReadOnlyList<LessonSentenceViewModel>> LoadSentencesAsync(
        string? contentUrl,
        CancellationToken cancellationToken = default);

    Task SaveTranscriptAsync(
        string contentUrl,
        IReadOnlyList<LessonSentenceViewModel> sentences,
        CancellationToken cancellationToken = default);

    string? ResolveWebRootPath(string? contentUrl);
}
