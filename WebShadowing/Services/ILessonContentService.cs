using WebShadowing.Models;

namespace WebShadowing.Services;

public interface ILessonContentService
{
    Task<IReadOnlyList<LessonSentenceDto>> GetSentencesAsync(
        long lessonId,
        IReadOnlyCollection<LessonMaterial> materials,
        CancellationToken cancellationToken = default);

    Task<bool> HasSentencesAsync(
        long lessonId,
        IReadOnlyCollection<LessonMaterial> materials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks only the local transcript file (no DB query).
    /// Used when DB sentence availability has already been batch-loaded.
    /// </summary>
    Task<bool> HasTranscriptAsync(IReadOnlyCollection<LessonMaterial> materials, CancellationToken cancellationToken = default);
}
