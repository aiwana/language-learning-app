namespace WebShadowing.Services;

public sealed record OpenAiChatMessage(string Role, string Content);

public interface IOpenAiApiClient
{
    bool IsConfigured { get; }
    Task<string> GenerateJsonAsync(string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
    Task<string> GenerateTextAsync(string model, IReadOnlyList<OpenAiChatMessage> messages, CancellationToken cancellationToken = default);
    Task<byte[]> CreateSpeechAsync(string model, string voice, string text, CancellationToken cancellationToken = default);
    Task<string> TranscribeAsync(string model, byte[] audio, string fileName, string contentType, CancellationToken cancellationToken = default);
}

public sealed class OpenAiServiceUnavailableException : Exception
{
    public OpenAiServiceUnavailableException(string message) : base(message) { }
    public OpenAiServiceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}
