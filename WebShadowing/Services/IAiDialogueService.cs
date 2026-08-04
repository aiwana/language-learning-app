using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IAiDialogueService
{
    Task<DialogueSessionDto> StartAsync(long userId, long? lessonId, CancellationToken cancellationToken = default);
    Task<DialogueSessionDto?> GetAsync(long userId, long sessionId, CancellationToken cancellationToken = default);
    Task<DialogueReplyDto?> SendTextAsync(long userId, long sessionId, string message, CancellationToken cancellationToken = default);
    Task<DialogueReplyDto?> SendAudioAsync(long userId, long sessionId, byte[] audio, string fileName, string contentType, CancellationToken cancellationToken = default);
}
