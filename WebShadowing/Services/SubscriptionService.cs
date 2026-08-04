using Microsoft.EntityFrameworkCore;
// Chức năng: nguồn trạng thái subscription/VIP và thao tác hủy gia hạn.
// Phụ trách chính: Minh. Hải Anh dùng read model này trên trang Tài khoản.
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    public SubscriptionService(AppDbContext db) => _db = db;

    public async Task<SubscriptionDto?> GetCurrentAsync(long userId, CancellationToken cancellationToken = default)
    {
        var item = await _db.VipSubscriptions.AsNoTracking().Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<bool> CancelRenewalAsync(long userId, CancellationToken cancellationToken = default)
    {
        var item = await _db.VipSubscriptions.Where(subscription => subscription.UserId == userId &&
                (subscription.Status == SubscriptionStatuses.Active || subscription.Status == SubscriptionStatuses.Pending))
            .OrderByDescending(subscription => subscription.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (item is null) return false;
        item.AutoRenew = false;
        item.CancelledAt = DateTime.UtcNow;
        if (item.Status == SubscriptionStatuses.Pending) item.Status = SubscriptionStatuses.Cancelled;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ActivateAsync(long transactionId, string providerReference, DateTime activatedAt, CancellationToken cancellationToken = default)
    {
        var transaction = await _db.PaymentTransactions.Include(item => item.Subscription).Include(item => item.User)
            .SingleOrDefaultAsync(item => item.PaymentTransactionId == transactionId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy giao dịch.");
        if (transaction.Status == PaymentStatuses.Succeeded) return;
        transaction.Status = PaymentStatuses.Succeeded;
        transaction.ProcessedAt = activatedAt;
        // Keep the merchant order id as the stable lookup key so duplicate
        // provider webhooks remain idempotent. Signature verification is the
        // authorization signal for the provider callback.
        var subscription = transaction.Subscription ?? throw new InvalidOperationException("Giao dịch chưa gắn subscription.");
        subscription.Status = SubscriptionStatuses.Active;
        subscription.StartsAt = activatedAt;
        subscription.EndsAt = subscription.BillingPeriod == BillingPeriods.Yearly ? activatedAt.AddYears(1) : activatedAt.AddMonths(1);
        subscription.UpdatedAt = activatedAt;
        transaction.User.IsVip = true;
        transaction.User.UpdatedAt = activatedAt;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var due = await _db.VipSubscriptions.Where(item => item.Status == SubscriptionStatuses.Active && item.EndsAt <= now)
            .ToListAsync(cancellationToken);
        foreach (var item in due) { item.Status = SubscriptionStatuses.Expired; item.UpdatedAt = now; }
        var userIds = due.Select(item => item.UserId).Distinct().ToList();
        foreach (var userId in userIds)
        {
            var hasOther = await _db.VipSubscriptions.AnyAsync(item => item.UserId == userId && item.Status == SubscriptionStatuses.Active && item.EndsAt > now, cancellationToken);
            if (!hasOther)
            {
                var user = await _db.Users.SingleAsync(item => item.UserId == userId, cancellationToken);
                user.IsVip = false;
                user.UpdatedAt = now;
            }
        }
        if (due.Count > 0) await _db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    public async Task<AdminActionResult> AdminGrantAsync(
        long userId,
        string billingPeriod,
        int? customDays,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return AdminActionResult.Fail("Không tìm thấy tài khoản.");
        }

        var now = DateTime.UtcNow;
        DateTime? endsAt = customDays is > 0
            ? now.AddDays(customDays.Value)
            : billingPeriod switch
            {
                BillingPeriods.Yearly => now.AddYears(1),
                BillingPeriods.Lifetime => null,
                _ => now.AddMonths(1)
            };

        var active = await _db.VipSubscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatuses.Active)
            .ToListAsync(cancellationToken);
        foreach (var item in active)
        {
            item.Status = SubscriptionStatuses.Cancelled;
            item.CancelledAt = now;
            item.AutoRenew = false;
            item.UpdatedAt = now;
        }

        var period = billingPeriod is BillingPeriods.Monthly or BillingPeriods.Yearly or BillingPeriods.Lifetime
            ? billingPeriod
            : BillingPeriods.Monthly;

        _db.VipSubscriptions.Add(new VipSubscription
        {
            UserId = userId,
            PlanCode = "admin_grant",
            BillingPeriod = period,
            Status = SubscriptionStatuses.Active,
            Provider = "admin",
            ProviderSubscriptionId = $"admin-{userId}-{now:yyyyMMddHHmmss}",
            StartsAt = now,
            EndsAt = endsAt,
            AutoRenew = false,
            CreatedAt = now,
            UpdatedAt = now
        });

        user.IsVip = true;
        user.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return AdminActionResult.Ok("Đã cấp VIP.");
    }

    public async Task<AdminActionResult> AdminRevokeAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return AdminActionResult.Fail("Không tìm thấy tài khoản.");
        }

        var now = DateTime.UtcNow;
        var active = await _db.VipSubscriptions
            .Where(s => s.UserId == userId &&
                        (s.Status == SubscriptionStatuses.Active || s.Status == SubscriptionStatuses.Pending))
            .ToListAsync(cancellationToken);

        foreach (var item in active)
        {
            item.Status = SubscriptionStatuses.Cancelled;
            item.CancelledAt = now;
            item.AutoRenew = false;
            item.UpdatedAt = now;
            if (item.EndsAt is null || item.EndsAt > now)
            {
                item.EndsAt = now;
            }
        }

        user.IsVip = false;
        user.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return AdminActionResult.Ok(active.Count > 0 ? "Đã thu hồi VIP." : "Tài khoản không có VIP đang hoạt động.");
    }

    private static SubscriptionDto ToDto(VipSubscription item) => new(item.SubscriptionId, item.PlanCode, item.BillingPeriod,
        item.Status, item.Provider, item.StartsAt, item.EndsAt, item.AutoRenew);
}
