using WebShadowing.Models;

namespace WebShadowing.Services;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetCurrentAsync(long userId, CancellationToken cancellationToken = default);
    Task<bool> CancelRenewalAsync(long userId, CancellationToken cancellationToken = default);
    Task ActivateAsync(long transactionId, string providerReference, DateTime activatedAt, CancellationToken cancellationToken = default);
    Task<int> ExpireDueAsync(CancellationToken cancellationToken = default);
    Task<AdminActionResult> AdminGrantAsync(long userId, string billingPeriod, int? customDays, CancellationToken cancellationToken = default);
    Task<AdminActionResult> AdminRevokeAsync(long userId, CancellationToken cancellationToken = default);
}
