using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class SubscriptionServiceTests
{
    [Fact]
    public async Task ActivateAsync_IsIdempotent_AndEnablesVip()
    {
        await using var db = CreateDb();
        var user = CreateUser();
        var subscription = CreateSubscription(user, SubscriptionStatuses.Pending);
        var transaction = CreateTransaction(user, subscription);
        db.AddRange(user, subscription, transaction);
        await db.SaveChangesAsync();
        var service = new SubscriptionService(db);
        var activatedAt = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);

        await service.ActivateAsync(transaction.PaymentTransactionId, "provider-1", activatedAt);
        var firstEnd = subscription.EndsAt;
        await service.ActivateAsync(transaction.PaymentTransactionId, "provider-1", activatedAt.AddMinutes(5));

        Assert.True(user.IsVip);
        Assert.Equal(PaymentStatuses.Succeeded, transaction.Status);
        Assert.Equal(SubscriptionStatuses.Active, subscription.Status);
        Assert.Equal(firstEnd, subscription.EndsAt);
    }

    [Fact]
    public async Task CancelRenewal_KeepsPaidEntitlementUntilExpiry()
    {
        await using var db = CreateDb();
        var user = CreateUser();
        user.IsVip = true;
        var subscription = CreateSubscription(user, SubscriptionStatuses.Active);
        subscription.AutoRenew = true;
        subscription.EndsAt = DateTime.UtcNow.AddMonths(1);
        db.AddRange(user, subscription);
        await db.SaveChangesAsync();
        var service = new SubscriptionService(db);

        Assert.True(await service.CancelRenewalAsync(user.UserId));

        Assert.True(user.IsVip);
        Assert.Equal(SubscriptionStatuses.Active, subscription.Status);
        Assert.False(subscription.AutoRenew);
        Assert.NotNull(subscription.CancelledAt);
    }

    [Fact]
    public async Task ExpireDueAsync_DisablesVipWhenNoOtherActiveSubscriptionExists()
    {
        await using var db = CreateDb();
        var user = CreateUser();
        user.IsVip = true;
        var subscription = CreateSubscription(user, SubscriptionStatuses.Active);
        subscription.StartsAt = DateTime.UtcNow.AddMonths(-2);
        subscription.EndsAt = DateTime.UtcNow.AddMinutes(-1);
        db.AddRange(user, subscription);
        await db.SaveChangesAsync();
        var service = new SubscriptionService(db);

        Assert.Equal(1, await service.ExpireDueAsync());

        Assert.False(user.IsVip);
        Assert.Equal(SubscriptionStatuses.Expired, subscription.Status);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static User CreateUser() => new()
    {
        Username = "payment-user",
        Email = "payment@example.com",
        PasswordHash = "hash",
        FullName = "Payment User",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static VipSubscription CreateSubscription(User user, string status) => new()
    {
        User = user,
        PlanCode = "vip_monthly",
        BillingPeriod = BillingPeriods.Monthly,
        Status = status,
        Provider = PaymentProviders.Momo,
        ProviderSubscriptionId = Guid.NewGuid().ToString("N"),
        StartsAt = DateTime.UtcNow,
        EndsAt = DateTime.UtcNow.AddMonths(1),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static PaymentTransaction CreateTransaction(User user, VipSubscription subscription) => new()
    {
        User = user,
        Subscription = subscription,
        Provider = PaymentProviders.Momo,
        ProviderTransactionId = Guid.NewGuid().ToString("N"),
        IdempotencyKey = Guid.NewGuid().ToString("N"),
        TransactionType = PaymentTypes.Purchase,
        Status = PaymentStatuses.Pending,
        Amount = 99_000,
        Currency = "VND"
    };
}
