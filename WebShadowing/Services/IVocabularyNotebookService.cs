using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IVocabularyNotebookService
{
    Task<VocabularyPageDto> GetPageAsync(
        long userId,
        int page,
        int? pageSize,
        CancellationToken cancellationToken = default);

    Task<VocabularyItemDto> UpsertAsync(
        long userId,
        AddVocabularyRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        long userId,
        long vocabularyItemId,
        CancellationToken cancellationToken = default);
}