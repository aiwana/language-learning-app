using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IAiLessonGenerationService
{
    Task<AiLessonPreviewDto> GenerateAsync(long userId, GenerateAiLessonRequestDto request, CancellationToken cancellationToken = default);
    Task<AiLessonPreviewDto?> GetPreviewAsync(long userId, Guid previewId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiLessonPreviewDto>> ListPreviewsAsync(long userId, CancellationToken cancellationToken = default);
    Task<SavedAiLessonDto?> GetSavedAsync(long userId, long savedLessonId, CancellationToken cancellationToken = default);
    Task<SavedAiLessonDto?> SaveAsync(long userId, Guid previewId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedAiLessonDto>> ListAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long userId, long savedLessonId, CancellationToken cancellationToken = default);
    Task<bool> DeletePreviewAsync(long userId, Guid previewId, CancellationToken cancellationToken = default);
}
