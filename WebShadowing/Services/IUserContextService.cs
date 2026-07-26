namespace WebShadowing.Services;

public interface IUserContextService
{
    bool IsAuthenticated { get; }
    long? GetCurrentUserId();
    Task<string> GetLearningModeAsync(CancellationToken cancellationToken = default);
    Task<byte> GetPronunciationTargetAsync(CancellationToken cancellationToken = default);
    Task<string> GetAccentAsync(CancellationToken cancellationToken = default);
}
