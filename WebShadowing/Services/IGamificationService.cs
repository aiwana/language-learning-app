using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IGamificationService
{
    Task<GamificationTransactionDto> ProcessVerifiedAttemptAsync(
        VerifiedPracticeAttempt attempt,
        CancellationToken cancellationToken = default);

    Task<GamificationTransactionDto> ExchangeHeartAsync(
        long userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<GamificationBalanceDto?> GetBalanceAsync(
        long userId,
        CancellationToken cancellationToken = default);
}
