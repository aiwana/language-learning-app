using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class OpenAiPronunciationAssessmentService : IPronunciationAssessmentService
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private const string UnavailableMessage = "Chưa kết nối AI, vui lòng kiểm tra lại!";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiPronunciationAssessmentService> _logger;

    public OpenAiPronunciationAssessmentService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAiPronunciationAssessmentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ShadowingEvaluationDto> AssessAsync(
        PronunciationAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OPENAI_API_KEY"] ?? _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new PronunciationAssessmentUnavailableException(UnavailableMessage);
        }

        try
        {
            var model = _configuration["OpenAI:AudioModel"] ?? "gpt-audio";
            var payload = new
            {
                model,
                modalities = new[] { "text" },
                temperature = 0.1,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "You are an English pronunciation coach. Listen to the learner audio, compare it with the target sentence, and return only valid JSON. Judge pronunciation accuracy, intelligibility, stress, rhythm, and fluency. Do not reward a merely plausible transcript. Feedback must be concise Vietnamese. JSON shape: {\"score\":0,\"transcript\":\"what was actually heard\",\"feedback\":\"Vietnamese feedback\",\"words\":[{\"word\":\"target word\",\"accuracyCode\":\"correct|warning|incorrect\",\"correction\":\"short Vietnamese advice or null\"}]}"
                    },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = $"Target sentence: {request.TargetText}\nTarget IPA: {request.TargetIpa ?? "not provided"}\nScore strictly from 0 to 100 and include every target word in order."
                            },
                            new
                            {
                                type = "input_audio",
                                input_audio = new
                                {
                                    data = Convert.ToBase64String(request.Audio),
                                    format = request.AudioFormat
                                }
                            }
                        }
                    }
                }
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            message.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var client = _httpClientFactory.CreateClient(nameof(OpenAiPronunciationAssessmentService));
            using var response = await client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI pronunciation request failed with status {StatusCode}.", response.StatusCode);
                throw new PronunciationAssessmentUnavailableException(UnavailableMessage);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var content = ExtractMessageContent(responseJson);
            var parsed = ParseAssessment(content, request);
            return parsed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "OpenAI pronunciation assessment failed.");
            throw new PronunciationAssessmentUnavailableException(UnavailableMessage, exception);
        }
    }

    private static ShadowingEvaluationDto ParseAssessment(
        string content,
        PronunciationAssessmentRequest request)
    {
        var json = StripCodeFence(content);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var score = Math.Clamp(root.GetProperty("score").GetInt32(), 0, 100);
        var transcript = root.TryGetProperty("transcript", out var transcriptNode)
            ? transcriptNode.GetString() ?? string.Empty
            : string.Empty;
        var feedback = root.TryGetProperty("feedback", out var feedbackNode)
            ? feedbackNode.GetString() ?? string.Empty
            : string.Empty;

        var words = new List<WordFeedbackDto>();
        if (root.TryGetProperty("words", out var wordsNode) && wordsNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in wordsNode.EnumerateArray())
            {
                var accuracyCode = item.TryGetProperty("accuracyCode", out var accuracyNode)
                    ? NormalizeAccuracy(accuracyNode.GetString())
                    : "warning";
                words.Add(new WordFeedbackDto
                {
                    Word = item.TryGetProperty("word", out var wordNode) ? wordNode.GetString() ?? string.Empty : string.Empty,
                    AccuracyCode = accuracyCode,
                    Correction = item.TryGetProperty("correction", out var correctionNode) && correctionNode.ValueKind != JsonValueKind.Null
                        ? correctionNode.GetString()
                        : null
                });
            }
        }

        return new ShadowingEvaluationDto
        {
            Score = score,
            Passed = score >= request.PronunciationTarget,
            PronunciationTarget = request.PronunciationTarget,
            Transcript = transcript,
            Feedback = string.IsNullOrWhiteSpace(feedback) ? "Hãy nghe lại câu mẫu và chú ý nhịp điệu của cả câu." : feedback,
            Words = words
        };
    }

    private static string ExtractMessageContent(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? throw new JsonException("OpenAI returned empty content.");
    }

    private static string StripCodeFence(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal)) return trimmed;
        var firstLineEnd = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstLineEnd >= 0 && lastFence > firstLineEnd
            ? trimmed[(firstLineEnd + 1)..lastFence].Trim()
            : trimmed;
    }

    private static string NormalizeAccuracy(string? value) => value?.ToLowerInvariant() switch
    {
        "correct" => "correct",
        "incorrect" => "incorrect",
        _ => "warning"
    };

}
