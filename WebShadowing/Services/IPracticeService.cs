namespace WebShadowing.Services;

public interface IPracticeService
{
    Task<PracticeSessionResult> StartSessionAsync(long userId, long lessonId, CancellationToken cancellationToken = default);
    Task<PracticeSessionResult> CompleteSessionAsync(long userId, long sessionId, decimal overallScore, CancellationToken cancellationToken = default);
}

public sealed record PracticeSessionResult(bool Succeeded, long? SessionId = null, string? ErrorMessage = null);
