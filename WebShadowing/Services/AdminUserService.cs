using Microsoft.EntityFrameworkCore;
// Chức năng: tìm kiếm/xem user, disable login, grant/revoke VIP và aggregate feature usage cho admin.
// Phụ trách chính: Minh.
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class AdminUserService : IAdminUserService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private static readonly TimeSpan UsageWindow = TimeSpan.FromDays(30);

    private readonly AppDbContext _db;
    private readonly ISubscriptionService _subscriptions;

    public AdminUserService(AppDbContext db, ISubscriptionService subscriptions)
    {
        _db = db;
        _subscriptions = subscriptions;
    }

    public async Task<AdminUserSearchResult> SearchUsersAsync(
        string? query,
        string? role,
        bool? isActive,
        bool? isVip,
        int page,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var users = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLowerInvariant();
            users = users.Where(u =>
                u.Email.ToLower().Contains(q)
                || u.Username.ToLower().Contains(q)
                || u.FullName.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            users = users.Where(u => u.Role == role);
        }

        if (isActive.HasValue)
        {
            users = users.Where(u => u.IsActive == isActive.Value);
        }

        if (isVip.HasValue)
        {
            users = users.Where(u => u.IsVip == isVip.Value);
        }

        var total = await users.CountAsync(cancellationToken);
        var pageUsers = await users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                u.Username,
                u.Role,
                u.LearningMode,
                u.IsVip,
                u.IsActive,
                LastPracticeAt = u.Statistics != null ? u.Statistics.LastPracticeAt : null
            })
            .ToListAsync(cancellationToken);

        var userIds = pageUsers.Select(u => u.UserId).ToList();
        var since = DateTime.UtcNow.Subtract(UsageWindow);
        var tabRows = await _db.PracticeAttempts.AsNoTracking()
            .Where(a => userIds.Contains(a.UserId) && a.AttemptedAt >= since)
            .GroupBy(a => new { a.UserId, a.PracticeTab })
            .Select(g => new { g.Key.UserId, g.Key.PracticeTab, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var tabsByUser = tabRows
            .GroupBy(r => r.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.OrderByDescending(x => x.Count)
                    .Select(x => x.PracticeTab)
                    .Distinct()
                    .Take(4)
                    .ToList());

        var items = pageUsers.Select(u => new AdminUserListItemDto(
            u.UserId,
            u.FullName,
            u.Email,
            u.Username,
            u.Role,
            u.LearningMode,
            u.IsVip,
            u.IsActive,
            u.LastPracticeAt,
            tabsByUser.GetValueOrDefault(u.UserId, Array.Empty<string>()))).ToList();

        return new AdminUserSearchResult(items, total, page, pageSize);
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Statistics)
            .SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var since = DateTime.UtcNow.Subtract(UsageWindow);
        var tabCounts = await _db.PracticeAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.AttemptedAt >= since)
            .GroupBy(a => a.PracticeTab)
            .Select(g => new { Tab = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dialogueCount = await _db.AiDialogueSessions.AsNoTracking()
            .CountAsync(s => s.UserId == userId && s.CreatedAt >= since, cancellationToken);
        var savedLessons = await _db.SavedAiLessons.AsNoTracking()
            .CountAsync(s => s.UserId == userId && s.CreatedAt >= since, cancellationToken);

        var allTabs = new[]
        {
            PracticeTabs.Shadowing,
            PracticeTabs.Dictation,
            PracticeTabs.IpaMatch,
            PracticeTabs.AiDialogue
        };
        var countMap = allTabs.ToDictionary(
            tab => tab,
            tab => tabCounts.FirstOrDefault(c => c.Tab == tab)?.Count ?? 0);

        var stats = user.Statistics;
        var usage = new AdminUserUsageDto(
            stats?.LastPracticeAt,
            stats?.Hearts ?? 0,
            stats?.Exp ?? 0,
            stats?.StreakDays ?? 0,
            stats?.TotalSessions ?? 0,
            stats?.AverageScore ?? 0,
            countMap,
            dialogueCount,
            savedLessons);

        var subscription = await _subscriptions.GetCurrentAsync(userId, cancellationToken);

        var audits = await _db.AdminAuditLogs.AsNoTracking()
            .Where(a => a.TargetUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .Join(_db.Users.AsNoTracking(), a => a.ActorUserId, u => u.UserId,
                (a, actor) => new AdminAuditItemDto(
                    a.AuditId,
                    a.ActorUserId,
                    actor.Email,
                    a.Action,
                    a.Detail,
                    a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminUserDetailDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.Username,
            user.Role,
            user.LearningMode,
            user.PronunciationTarget,
            user.Accent,
            user.IsVip,
            user.IsActive,
            user.OnboardingCompleted,
            user.CreatedAt,
            user.UpdatedAt,
            user.DisabledAt,
            user.DisabledReason,
            user.DisabledByUserId,
            subscription,
            usage,
            audits);
    }

    public async Task<AdminActionResult> SetActiveAsync(
        long actorUserId,
        long targetUserId,
        bool active,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == targetUserId)
        {
            return AdminActionResult.Fail("Không thể tự vô hiệu hóa hoặc kích hoạt tài khoản của chính mình.");
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == targetUserId, cancellationToken);
        if (user is null)
        {
            return AdminActionResult.Fail("Không tìm thấy tài khoản.");
        }

        if (user.IsActive == active)
        {
            return AdminActionResult.Ok(active ? "Tài khoản đã đang hoạt động." : "Tài khoản đã bị vô hiệu hóa.");
        }

        var now = DateTime.UtcNow;
        user.IsActive = active;
        user.UpdatedAt = now;
        if (active)
        {
            user.DisabledAt = null;
            user.DisabledReason = null;
            user.DisabledByUserId = null;
        }
        else
        {
            user.DisabledAt = now;
            user.DisabledReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            user.DisabledByUserId = actorUserId;
        }

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = active ? AdminAuditActions.EnableUser : AdminAuditActions.DisableUser,
            Detail = active ? null : user.DisabledReason,
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return AdminActionResult.Ok(active ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản.");
    }

    public async Task<AdminActionResult> GrantVipAsync(
        long actorUserId,
        long targetUserId,
        string billingPeriod,
        int? customDays,
        CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = billingPeriod?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedPeriod is not (BillingPeriods.Monthly or BillingPeriods.Yearly or BillingPeriods.Lifetime)
            && customDays is null or <= 0)
        {
            return AdminActionResult.Fail("Kỳ VIP không hợp lệ.");
        }

        if (customDays is > 0)
        {
            normalizedPeriod = BillingPeriods.Monthly;
        }

        var result = await _subscriptions.AdminGrantAsync(
            targetUserId, normalizedPeriod, customDays, cancellationToken);
        if (!result.Succeeded)
        {
            return result;
        }

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = AdminAuditActions.GrantVip,
            Detail = customDays is > 0
                ? $"days:{customDays}"
                : $"period:{normalizedPeriod}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return AdminActionResult.Ok("Đã cấp VIP.");
    }

    public async Task<AdminActionResult> RevokeVipAsync(
        long actorUserId,
        long targetUserId,
        CancellationToken cancellationToken = default)
    {
        var result = await _subscriptions.AdminRevokeAsync(targetUserId, cancellationToken);
        if (!result.Succeeded)
        {
            return result;
        }

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = AdminAuditActions.RevokeVip,
            Detail = null,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return AdminActionResult.Ok("Đã thu hồi VIP.");
    }
}
