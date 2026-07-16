using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class UserContextService : IUserContextService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private UserPreferences? _cachedPreferences;

    public UserContextService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public long? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return long.TryParse(claim?.Value, out var id) ? id : null;
    }

    public async Task<string> GetLearningModeAsync(CancellationToken cancellationToken = default)
    {
        return (await GetPreferencesAsync(cancellationToken)).LearningMode;
    }

    public async Task<byte> GetPronunciationTargetAsync(CancellationToken cancellationToken = default)
    {
        return (await GetPreferencesAsync(cancellationToken)).PronunciationTarget;
    }

    private async Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken)
    {
        if (_cachedPreferences is not null)
        {
            return _cachedPreferences;
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return _cachedPreferences = new UserPreferences(
                LearningModes.Casual,
                PronunciationTargets.Comprehension70);
        }

        var preferences = await _db.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId.Value)
            .Select(user => new
            {
                user.LearningMode,
                PronunciationTarget = (byte?)user.PronunciationTarget
            })
            .FirstOrDefaultAsync(cancellationToken);

        return _cachedPreferences = new UserPreferences(
            NormalizeMode(preferences?.LearningMode),
            preferences?.PronunciationTarget ?? PronunciationTargets.Comprehension70);
    }

    private static string NormalizeMode(string? mode)
    {
        return mode?.Trim().ToLowerInvariant() switch
        {
            LearningModes.Academic => LearningModes.Academic,
            LearningModes.Professional => LearningModes.Professional,
            _ => LearningModes.Casual
        };
    }

    private sealed record UserPreferences(string LearningMode, byte PronunciationTarget);
}
