using Microsoft.EntityFrameworkCore;
// Chức năng: read model streak, tim, EXP và VIP cho navbar/trang Tiến trình.
// Phụ trách chính: Hải Anh. Rule cập nhật số liệu nằm ở GamificationService do Minh phối hợp.
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class UserStatsService : IUserStatsService
{
    private readonly AppDbContext _db;
    private readonly IUserContextService _userContext;

    public UserStatsService(AppDbContext db, IUserContextService userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<UserNavStatsViewModel?> GetNavStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAuthenticated)
        {
            return null;
        }

        var userId = _userContext.GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        var data = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId.Value)
            .Select(u => new
            {
                u.IsVip,
                Streak = u.Statistics != null ? u.Statistics.StreakDays : 0,
                Hearts = u.Statistics != null ? u.Statistics.Hearts : 0,
                Exp    = u.Statistics != null ? u.Statistics.Exp    : 0,
                CompletedSentences = _db.UserLessonProgress
                    .Where(progress => progress.UserId == u.UserId)
                    .Sum(progress => progress.CompletedSentenceCount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            return null;
        }

        return new UserNavStatsViewModel
        {
            Streak  = data.Streak,
            Hearts  = data.Hearts,
            Exp     = data.Exp,
            CompletedSentences = data.CompletedSentences,
            IsVip   = data.IsVip
        };
    }
}
