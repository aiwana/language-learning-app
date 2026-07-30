using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IUserStatsService
{
    /// <summary>
    /// Returns nav-bar stats for the currently authenticated user,
    /// or <c>null</c> when the user is not authenticated.
    /// </summary>
    Task<UserNavStatsViewModel?> GetNavStatsAsync(CancellationToken cancellationToken = default);
}
