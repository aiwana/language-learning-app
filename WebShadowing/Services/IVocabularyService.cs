using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IVocabularyService
{
    Task<VocabularyPageDto> GetPageAsync(long userId, string? status, int page, int? pageSize = null, CancellationToken cancellationToken = default);
    Task<VocabularyItemDto?> GetAsync(long userId, long itemId, CancellationToken cancellationToken = default);
    Task<VocabularyItemDto> AddAsync(long userId, AddVocabularyRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> MarkMasteredAsync(long userId, long itemId, CancellationToken cancellationToken = default);
    Task<bool> ResetReviewAsync(long userId, long itemId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long userId, long itemId, CancellationToken cancellationToken = default);
}
