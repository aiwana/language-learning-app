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
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return LearningModes.Casual;
        }

        var mode = await _db.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId.Value)
            .Select(user => user.LearningMode)
            .FirstOrDefaultAsync(cancellationToken);

        return NormalizeMode(mode);
    }

    public async Task<byte> GetPronunciationTargetAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return PronunciationTargets.Comprehension70;
        }

        var target = await _db.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId.Value)
            .Select(user => (byte?)user.PronunciationTarget)
            .FirstOrDefaultAsync(cancellationToken);

        return target ?? PronunciationTargets.Comprehension70;
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
}
