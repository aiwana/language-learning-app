using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class OpenAiPronunciationAssessmentService : IPronunciationAssessmentProvider
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private const string UnavailableMessage = "Chưa kết nối AI, vui lòng kiểm tra lại!";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly PronunciationAssessmentOptions _options;
    private readonly ILogger<OpenAiPronunciationAssessmentService> _logger;

    public string ProviderName => _options.OpenAiProviderName;

    public OpenAiPronunciationAssessmentService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<PronunciationAssessmentOptions> options,
        ILogger<OpenAiPronunciationAssessmentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PronunciationAssessmentResult> AssessAsync(
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
            var model = _configuration["OpenAI:AudioModel"] ?? "gpt-audio-1.5";
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
                        content = "You are an English pronunciation coach. Listen to the learner audio and compare with the target sentence. Return valid JSON only with shape: {\"score\":0,\"accuracyScore\":0,\"fluencyScore\":0,\"completenessScore\":0,\"prosodyScore\":0,\"transcript\":\"what was heard\",\"feedback\":\"Vietnamese feedback\",\"words\":[{\"word\":\"target word\",\"accuracyCode\":\"correct|warning|incorrect\",\"correction\":\"short Vietnamese advice or null\",\"phonemes\":[{\"symbol\":\"phoneme\",\"accuracyCode\":\"correct|warning|incorrect\"}]}]}"
                    },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = $"Target sentence: {request.TargetText}\nTarget IPA: {request.TargetIpa ?? "not provided"}\nAccent target: {request.Accent}\nLearning mode: {request.LearningMode}\nProvide score 0..100 and include every target word in order."
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
                if ((int)response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    throw new PronunciationAssessmentUnavailableException(
                        "OpenAI hết quota hoặc bị giới hạn tần suất.",
                        StatusCodes.Status429TooManyRequests,
                        "pronunciation_quota_exhausted");
                }

                if ((int)response.StatusCode >= StatusCodes.Status500InternalServerError)
                {
                    throw new PronunciationAssessmentUnavailableException(
                        UnavailableMessage,
                        StatusCodes.Status503ServiceUnavailable,
                        "pronunciation_provider_unavailable");
                }

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
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(exception, "OpenAI pronunciation assessment timed out.");
            throw new PronunciationAssessmentUnavailableException(
                "Provider chấm phát âm hết thời gian xử lý.",
                StatusCodes.Status504GatewayTimeout,
                "pronunciation_provider_timeout",
                exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "OpenAI pronunciation assessment failed.");
            throw new PronunciationAssessmentUnavailableException(UnavailableMessage, innerException: exception);
        }
    }

    private PronunciationAssessmentResult ParseAssessment(
        string content,
        PronunciationAssessmentRequest request)
    {
        var json = StripCodeFence(content);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var score = TryGetOptionalInt(root, "score") ?? 0;
        var accuracyScore = TryGetOptionalInt(root, "accuracyScore");
        var fluencyScore = TryGetOptionalInt(root, "fluencyScore");
        var completenessScore = TryGetOptionalInt(root, "completenessScore");
        var prosodyScore = TryGetOptionalInt(root, "prosodyScore");
        var transcript = root.TryGetProperty("transcript", out var transcriptNode)
            ? transcriptNode.GetString() ?? string.Empty
            : string.Empty;
        var feedback = root.TryGetProperty("feedback", out var feedbackNode)
            ? feedbackNode.GetString() ?? string.Empty
            : string.Empty;

        var words = new List<PronunciationWordResult>();
        if (root.TryGetProperty("words", out var wordsNode) && wordsNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in wordsNode.EnumerateArray())
            {
                var accuracyCode = item.TryGetProperty("accuracyCode", out var accuracyNode)
                    ? NormalizeAccuracy(accuracyNode.GetString())
                    : "warning";

                var phonemes = new List<PronunciationPhonemeResult>();
                if (item.TryGetProperty("phonemes", out var phonemeNode) && phonemeNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var phoneme in phonemeNode.EnumerateArray())
                    {
                        phonemes.Add(new PronunciationPhonemeResult
                        {
                            Symbol = phoneme.TryGetProperty("symbol", out var symbolNode)
                                ? symbolNode.GetString() ?? string.Empty
                                : string.Empty,
                            AccuracyCode = phoneme.TryGetProperty("accuracyCode", out var phonemeAccuracyNode)
                                ? NormalizeAccuracy(phonemeAccuracyNode.GetString())
                                : "warning"
                        });
                    }
                }

                words.Add(new PronunciationWordResult
                {
                    Word = item.TryGetProperty("word", out var wordNode) ? wordNode.GetString() ?? string.Empty : string.Empty,
                    AccuracyCode = accuracyCode,
                    Correction = item.TryGetProperty("correction", out var correctionNode) && correctionNode.ValueKind != JsonValueKind.Null
                        ? correctionNode.GetString()
                        : null,
                    Phonemes = phonemes
                });
            }
        }

        return new PronunciationAssessmentResult
        {
            Provider = ProviderName,
            OverallScore = score,
            AccuracyScore = accuracyScore,
            FluencyScore = fluencyScore,
            CompletenessScore = completenessScore,
            ProsodyScore = prosodyScore,
            Transcript = transcript,
            Feedback = string.IsNullOrWhiteSpace(feedback) ? "Hãy nghe lại câu mẫu và chú ý nhịp điệu của cả câu." : feedback,
            Words = words
        };
    }

    private static int? TryGetOptionalInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number => Math.Clamp((int)Math.Round(property.GetDouble(), MidpointRounding.AwayFromZero), 0, 100),
            JsonValueKind.String when double.TryParse(
                property.GetString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value) => Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 100),
            _ => null
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
