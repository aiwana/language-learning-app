using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IFavoriteSentenceService
{
    Task<IReadOnlyList<FavoriteSentenceDto>> ListAsync(long userId, CancellationToken cancellationToken = default);
    Task<FavoriteSentenceDto?> AddAsync(long userId, long sentenceId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long userId, long favoriteId, CancellationToken cancellationToken = default);
}
