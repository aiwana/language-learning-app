using WebShadowing.Models;

namespace WebShadowing.Services;

public interface ILanguageReferenceService
{
    Task<WordMeaningDto> GetMeaningAsync(
        string word,
        string? context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WordIpaDto>> GetIpaBatchAsync(
        IReadOnlyList<string> words,
        CancellationToken cancellationToken = default);
}
