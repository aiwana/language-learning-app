namespace WebShadowing.Services;

// Chức năng: background job đồng bộ subscription hết hạn với entitlement User.IsVip.
// Phụ trách chính: Minh. Khi scale nhiều instance cần distributed lock/scheduler riêng.
public sealed class SubscriptionExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpiryService> _logger;
    public SubscriptionExpiryService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionExpiryService> logger) { _scopeFactory = scopeFactory; _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                var count = await service.ExpireDueAsync(stoppingToken);
                if (count > 0) _logger.LogInformation("Expired {Count} VIP subscriptions.", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                if (stoppingToken.IsCancellationRequested) break;
                _logger.LogError(exception, "Failed to expire VIP subscriptions.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
