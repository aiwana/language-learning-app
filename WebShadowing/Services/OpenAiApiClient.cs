using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebShadowing.Services;

public sealed class OpenAiApiClient : IOpenAiApiClient
{
    private const string ChatEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string SpeechEndpoint = "https://api.openai.com/v1/audio/speech";
    private const string TranscriptionEndpoint = "https://api.openai.com/v1/audio/transcriptions";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiApiClient> _logger;

    public OpenAiApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OpenAiApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());

    public Task<string> GenerateJsonAsync(string model, string systemPrompt, string userPrompt, CancellationToken cancellationToken = default) =>
        SendChatAsync(model, [new("system", systemPrompt), new("user", userPrompt)], jsonMode: true, cancellationToken);

    public Task<string> GenerateTextAsync(string model, IReadOnlyList<OpenAiChatMessage> messages, CancellationToken cancellationToken = default) =>
        SendChatAsync(model, messages, jsonMode: false, cancellationToken);

    public async Task<byte[]> CreateSpeechAsync(string model, string voice, string text, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var payload = new { model, voice, input = text, response_format = "mp3" };
        using var request = CreateRequest(HttpMethod.Post, SpeechEndpoint);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var startedAt = Stopwatch.GetTimestamp();
        using var response = await Client().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        LogLatency("TTS", model, response.StatusCode, startedAt);
        await EnsureSuccessAsync(response, "TTS", cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<string> TranscribeAsync(string model, byte[] audio, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        using var request = CreateRequest(HttpMethod.Post, TranscriptionEndpoint);
        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(audio);
        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(contentType) ? "audio/wav" : contentType);
        content.Add(audioContent, "file", Path.GetFileName(fileName));
        content.Add(new StringContent(model), "model");
        content.Add(new StringContent("en"), "language");
        request.Content = content;
        var startedAt = Stopwatch.GetTimestamp();
        using var response = await Client().SendAsync(request, cancellationToken);
        LogLatency("speech-to-text", model, response.StatusCode, startedAt);
        await EnsureSuccessAsync(response, "speech-to-text", cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.TryGetProperty("text", out var textNode) ? textNode.GetString()?.Trim() ?? string.Empty : string.Empty;
    }

    private async Task<string> SendChatAsync(string model, IReadOnlyList<OpenAiChatMessage> messages, bool jsonMode, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["temperature"] = 0.3,
            ["messages"] = messages.Select(message => new { role = message.Role, content = message.Content }).ToArray()
        };
        if (jsonMode) payload["response_format"] = new { type = "json_object" };
        using var request = CreateRequest(HttpMethod.Post, ChatEndpoint);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var startedAt = Stopwatch.GetTimestamp();
        using var response = await Client().SendAsync(request, cancellationToken);
        LogLatency("text generation", model, response.StatusCode, startedAt);
        await EnsureSuccessAsync(response, "text generation", cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim()
            ?? throw new OpenAiServiceUnavailableException("OpenAI trả về nội dung trống.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetApiKey());
        return request;
    }

    private HttpClient Client() => _httpClientFactory.CreateClient(nameof(OpenAiApiClient));
    private string? GetApiKey() => _configuration["OPENAI_API_KEY"] ?? _configuration["OpenAI:ApiKey"];
    private void LogLatency(string operation, string model, System.Net.HttpStatusCode statusCode, long startedAt) =>
        _logger.LogInformation("OpenAI {Operation} using {Model} completed with {StatusCode} in {ElapsedMs} ms.",
            operation, model, (int)statusCode, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
    private void EnsureConfigured()
    {
        if (!IsConfigured) throw new OpenAiServiceUnavailableException("OPENAI_API_KEY chưa được cấu hình.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("OpenAI {Operation} failed with {StatusCode}: {Body}", operation, (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
        throw new OpenAiServiceUnavailableException($"Dịch vụ AI chưa sẵn sàng ({(int)response.StatusCode}).");
    }
}
