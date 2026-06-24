using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IDictionaryService
{
    Task<WordLookupResult> LookupWordAsync(string word, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, WordLookupResult>> LookupWordsAsync(
        IEnumerable<string> words,
        CancellationToken cancellationToken = default);
}
