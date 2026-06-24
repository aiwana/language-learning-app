using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IUserContextService
{
    Task<UserProfile> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<UserStats> GetStatsAsync(CancellationToken cancellationToken = default);
    long? GetCurrentUserId();
    bool IsAuthenticated { get; }
}
