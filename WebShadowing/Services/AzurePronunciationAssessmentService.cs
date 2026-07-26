using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class AzurePronunciationAssessmentService : IPronunciationAssessmentProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly PronunciationAssessmentOptions _options;
    private readonly ILogger<AzurePronunciationAssessmentService> _logger;

    public AzurePronunciationAssessmentService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<PronunciationAssessmentOptions> options,
        ILogger<AzurePronunciationAssessmentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => _options.AzureProviderName;

    public async Task<PronunciationAssessmentResult> AssessAsync(
        PronunciationAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.AudioFormat, "wav", StringComparison.OrdinalIgnoreCase))
        {
            throw new PronunciationAssessmentUnavailableException(
                "Provider chuyên dụng chỉ hỗ trợ định dạng WAV PCM cho luồng hiện tại.",
                StatusCodes.Status400BadRequest,
                "unsupported_audio_format");
        }

        var apiKey = _configuration["AzureSpeech:ApiKey"];
        var region = _configuration["AzureSpeech:Region"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(region))
        {
            throw new PronunciationAssessmentUnavailableException(
                "Provider chấm phát âm chuyên dụng chưa được cấu hình.",
                StatusCodes.Status503ServiceUnavailable,
                "pronunciation_provider_not_configured");
        }

        var accent = request.Accent switch
        {
            Accents.EnGb => "en-GB",
            _ => "en-US"
        };

        var endpoint = $"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={accent}&format=detailed";
        var pronunciationConfig = new
        {
            ReferenceText = request.TargetText,
            GradingSystem = "HundredMark",
            Granularity = "Phoneme",
            Dimension = "Comprehensive",
            EnableMiscue = true
        };
        var pronunciationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pronunciationConfig)));

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new ByteArrayContent(request.Audio)
        };
        message.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", apiKey);
        message.Headers.TryAddWithoutValidation("Pronunciation-Assessment", pronunciationHeader);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        message.Content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("codecs", "audio/pcm"));
        message.Content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("samplerate", "16000"));

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, _options.ProviderTimeoutSeconds)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(AzurePronunciationAssessmentService));
            using var response = await client.SendAsync(message, linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure pronunciation request failed with status {StatusCode}.", response.StatusCode);
                if ((int)response.StatusCode == StatusCodes.Status429TooManyRequests)
                {
                    throw new PronunciationAssessmentUnavailableException(
                        "Provider chấm phát âm đã hết quota hoặc vượt ngưỡng gọi API.",
                        StatusCodes.Status429TooManyRequests,
                        "pronunciation_quota_exhausted");
                }

                if ((int)response.StatusCode == StatusCodes.Status408RequestTimeout)
                {
                    throw new PronunciationAssessmentUnavailableException(
                        "Provider chấm phát âm hết thời gian xử lý.",
                        StatusCodes.Status504GatewayTimeout,
                        "pronunciation_provider_timeout");
                }

                throw new PronunciationAssessmentUnavailableException(
                    "Provider chấm phát âm tạm thời không khả dụng.",
                    StatusCodes.Status503ServiceUnavailable,
                    "pronunciation_provider_unavailable");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseAzureResponse(responseJson);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Provider chấm phát âm hết thời gian xử lý.",
                StatusCodes.Status504GatewayTimeout,
                "pronunciation_provider_timeout");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PronunciationAssessmentUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            _logger.LogWarning(exception, "Azure pronunciation assessment failed.");
            throw new PronunciationAssessmentUnavailableException(
                "Provider chấm phát âm tạm thời không khả dụng.",
                StatusCodes.Status503ServiceUnavailable,
                "pronunciation_provider_unavailable",
                exception);
        }
    }

    private PronunciationAssessmentResult ParseAzureResponse(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("NBest", out var nBestNode) || nBestNode.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Azure Speech returned no NBest payload.");
        }

        var first = nBestNode.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Azure Speech returned an invalid NBest payload.");
        }

        var transcript = first.TryGetProperty("Display", out var displayNode)
            ? displayNode.GetString() ?? string.Empty
            : string.Empty;

        var scoreNode = first.GetProperty("PronunciationAssessment");
        var overall = ToInt(scoreNode, "PronScore");
        var accuracy = ToNullableInt(scoreNode, "AccuracyScore");
        var fluency = ToNullableInt(scoreNode, "FluencyScore");
        var completeness = ToNullableInt(scoreNode, "CompletenessScore");
        var prosody = ToNullableInt(scoreNode, "ProsodyScore");

        var words = new List<PronunciationWordResult>();
        if (first.TryGetProperty("Words", out var wordsNode) && wordsNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var wordNode in wordsNode.EnumerateArray())
            {
                var text = wordNode.TryGetProperty("Word", out var textNode)
                    ? textNode.GetString() ?? string.Empty
                    : string.Empty;

                var errorType = wordNode.TryGetProperty("PronunciationAssessment", out var assessmentNode)
                    && assessmentNode.TryGetProperty("ErrorType", out var errorTypeNode)
                    ? errorTypeNode.GetString()
                    : null;

                var accuracyCode = errorType?.ToLowerInvariant() switch
                {
                    "none" => "correct",
                    "mispronunciation" => "incorrect",
                    "omission" => "incorrect",
                    "insertion" => "incorrect",
                    _ => "warning"
                };

                var phonemes = new List<PronunciationPhonemeResult>();
                if (wordNode.TryGetProperty("Phonemes", out var phonemeNode) && phonemeNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in phonemeNode.EnumerateArray())
                    {
                        var symbol = item.TryGetProperty("Phoneme", out var symbolNode)
                            ? symbolNode.GetString() ?? string.Empty
                            : string.Empty;

                        var phonemeAccuracy = item.TryGetProperty("PronunciationAssessment", out var paNode)
                            && paNode.TryGetProperty("AccuracyScore", out var paScoreNode)
                            && paScoreNode.ValueKind == JsonValueKind.Number
                            ? paScoreNode.GetDouble()
                            : 65d;

                        phonemes.Add(new PronunciationPhonemeResult
                        {
                            Symbol = symbol,
                            AccuracyCode = phonemeAccuracy >= 85 ? "correct" : phonemeAccuracy >= 65 ? "warning" : "incorrect"
                        });
                    }
                }

                words.Add(new PronunciationWordResult
                {
                    Word = text,
                    AccuracyCode = accuracyCode,
                    Correction = accuracyCode == "incorrect"
                        ? "Cần nhấn âm và khẩu hình rõ hơn ở từ này."
                        : null,
                    Phonemes = phonemes
                });
            }
        }

        return new PronunciationAssessmentResult
        {
            Provider = ProviderName,
            OverallScore = overall,
            AccuracyScore = accuracy,
            FluencyScore = fluency,
            CompletenessScore = completeness,
            ProsodyScore = prosody,
            Transcript = transcript,
            Feedback = BuildFeedback(overall, words),
            Words = words
        };
    }

    private static string BuildFeedback(int overall, IReadOnlyList<PronunciationWordResult> words)
    {
        if (words.Count == 0)
        {
            return overall >= 70
                ? "Bạn đọc khá rõ. Hãy giữ tốc độ ổn định và nối âm tự nhiên hơn."
                : "Hãy đọc chậm hơn, nhấn trọng âm chính và phát âm rõ phụ âm cuối.";
        }

        var incorrectCount = words.Count(item => item.AccuracyCode == "incorrect");
        if (incorrectCount == 0)
        {
            return "Phát âm tốt. Hãy tinh chỉnh nhịp điệu để tự nhiên hơn.";
        }

        return $"Có {incorrectCount} từ cần sửa. Tập trung vào trọng âm từ và âm cuối trước khi tăng tốc độ.";
    }

    private static int ToInt(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var valueNode) || valueNode.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(valueNode.GetDouble(), MidpointRounding.AwayFromZero), 0, 100);
    }

    private static int? ToNullableInt(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out var valueNode) || valueNode.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return Math.Clamp((int)Math.Round(valueNode.GetDouble(), MidpointRounding.AwayFromZero), 0, 100);
    }
}
