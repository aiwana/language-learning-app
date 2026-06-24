using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class UserContextService : IUserContextService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public long? GetCurrentUserId()
    {
        var userIdValue = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        return long.TryParse(userIdValue, out var userId) ? userId : null;
    }

    public async Task<UserProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return StaticData.DefaultProfile;
        }

        return new UserProfile
        {
            Name = user.FullName,
            Email = user.Email,
            Phone = string.Empty,
            Level = UserLevel.Casual,
            Goal = LearningGoal.Comprehension70,
            TargetAccent = TargetAccent.US,
            IsPremium = false
        };
    }

    public async Task<UserStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return StaticData.DefaultStats;
        }

        var stats = await _db.UserStatistics
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.UserId, cancellationToken);

        if (stats is null)
        {
            return StaticData.DefaultStats;
        }

        return new UserStats
        {
            Streak = stats.StreakDays,
            TotalSentences = stats.TotalSessions,
            Hearts = 5,
            Exp = (int)Math.Round(stats.AverageScore * 10, MidpointRounding.AwayFromZero)
        };
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userIdValue = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }
}
