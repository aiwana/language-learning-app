using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IFavoriteSentenceService
{
    Task<IReadOnlyList<FavoriteSentenceDto>> GetListAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<FavoriteSentenceStatusDto> GetStatusAsync(
        long userId,
        FavoriteSentenceStatusRequestDto request,
        CancellationToken cancellationToken = default);

    Task<FavoriteSentenceMutationDto> SaveAsync(
        long userId,
        AddFavoriteRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        long userId,
        long favoriteSentenceId,
        CancellationToken cancellationToken = default);
}