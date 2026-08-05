using System.Data;
// Chức năng: policy và transaction đổi learning mode, ghi lịch sử và tính phí/giới hạn.
// Phụ trách trang và test: Hải Anh. Minh review business rule và consistency DB.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class ModeChangeService : IModeChangeService
{
    private readonly AppDbContext _db;
    private readonly ModeChangeOptions _options;
    public ModeChangeService(AppDbContext db, IOptions<ModeChangeOptions> options) { _db = db; _options = options.Value; }

    public async Task<ModeChangeResultDto> ChangeAsync(long userId, ChangeLearningModeRequestDto request, CancellationToken cancellationToken = default)
    {
        var target = request.LearningMode.Trim().ToLowerInvariant();
        if (target is not (LearningModes.Casual or LearningModes.Academic or LearningModes.Professional))
            return new(false, target, false, 0, 0, 0, "Mode học không hợp lệ.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await _db.Users.Include(item => item.Statistics).SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null) return new(false, target, false, 0, 0, 0, "Không tìm thấy người dùng.");
        if (user.LearningMode == target)
            return new(true, target, true, 0, user.Statistics?.Exp ?? 0, await RemainingAsync(user, cancellationToken));

        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var used = await _db.ModeChangeHistory.CountAsync(item => item.UserId == userId && item.ChangedBy == ModeChangeActors.User && item.ChangedAt >= start, cancellationToken);
        var canUseFree = user.IsVip && _options.VipUnlimited || used < _options.FreeChangesPerMonth;
        var expCharged = 0;
        if (!canUseFree)
        {
            if (!request.UseExpIfNeeded)
                return new(false, user.LearningMode, false, 0, user.Statistics?.Exp ?? 0, 0, $"Bạn đã dùng hết lượt đổi miễn phí. Cần {_options.ExpCostPerChange} EXP.");
            if (user.Statistics is null || user.Statistics.Exp < _options.ExpCostPerChange)
                return new(false, user.LearningMode, false, 0, user.Statistics?.Exp ?? 0, 0, "Không đủ EXP để đổi mode.");
            user.Statistics.Exp -= _options.ExpCostPerChange;
            expCharged = _options.ExpCostPerChange;
        }

        var previous = user.LearningMode;
        user.LearningMode = target;
        user.UpdatedAt = DateTime.UtcNow;
        _db.ModeChangeHistory.Add(new ModeChangeHistory
        {
            UserId = userId, FromMode = previous, ToMode = target, ChangedBy = ModeChangeActors.User,
            Reason = expCharged > 0 ? $"exp:{expCharged}" : user.IsVip ? "vip" : "monthly_free", ChangedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, target, canUseFree, expCharged, user.Statistics?.Exp ?? 0, await RemainingAsync(user, cancellationToken));
    }

    private async Task<int> RemainingAsync(User user, CancellationToken cancellationToken)
    {
        if (user.IsVip && _options.VipUnlimited) return int.MaxValue;
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var used = await _db.ModeChangeHistory.CountAsync(item => item.UserId == user.UserId && item.ChangedBy == ModeChangeActors.User && item.ChangedAt >= start, cancellationToken);
        return Math.Max(0, _options.FreeChangesPerMonth - used);
    }
}
