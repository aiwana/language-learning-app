using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class OpenAiLanguageReferenceService : ILanguageReferenceService
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiLanguageReferenceService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly MemoryCacheEntryOptions CacheOptions = new MemoryCacheEntryOptions()
        .SetSize(1)
        .SetSlidingExpiration(TimeSpan.FromHours(1))
        .SetAbsoluteExpiration(TimeSpan.FromHours(6));

    public OpenAiLanguageReferenceService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<OpenAiLanguageReferenceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WordMeaningDto> GetMeaningAsync(
        string word,
        string? context,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeWord(word);
        var cacheKey = $"meaning:{normalized}|{context?.Trim()}";
        if (_cache.TryGetValue<WordMeaningDto>(cacheKey, out var cached) && cached is not null) return cached;

        var fallback = new WordMeaningDto
        {
            Word = word,
            Meaning = "Chưa tra được nghĩa của từ này. Vui lòng thử lại sau.",
            Provider = "fallback"
        };

        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey)) return fallback;

        try
        {
            var prompt = $"Return only JSON with keys word, ipa, meaning. Give US IPA wrapped in / / and a short Vietnamese meaning appropriate to this sentence. Word: {normalized}. Sentence: {context ?? "not provided"}.";
            var content = await AskTextModelAsync(apiKey, prompt, cancellationToken);
            using var document = JsonDocument.Parse(StripCodeFence(content));
            var root = document.RootElement;
            var result = new WordMeaningDto
            {
                Word = word,
                Ipa = root.TryGetProperty("ipa", out var ipaNode) ? ipaNode.GetString() ?? string.Empty : string.Empty,
                Meaning = root.TryGetProperty("meaning", out var meaningNode) ? meaningNode.GetString() ?? fallback.Meaning : fallback.Meaning,
                Provider = _configuration["OpenAI:TextModel"] ?? "gpt-4o-mini"
            };
            _cache.Set(cacheKey, result, CacheOptions);
            if (!string.IsNullOrWhiteSpace(result.Ipa)) SetCachedIpa(normalized, result.Ipa);
            return result;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Dictionary lookup failed for {Word}.", normalized);
            return fallback;
        }
    }

    public async Task<IReadOnlyList<WordIpaDto>> GetIpaBatchAsync(
        IReadOnlyList<string> words,
        CancellationToken cancellationToken = default)
    {
        var normalizedWords = words
            .Select(NormalizeWord)
            .Where(word => word.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToList();
        var results = new List<WordIpaDto>();
        var missing = new List<string>();
        foreach (var word in normalizedWords)
        {
            if (_cache.TryGetValue<string>(GetIpaCacheKey(word), out var ipa) && !string.IsNullOrWhiteSpace(ipa))
            {
                results.Add(new WordIpaDto { Word = word, Ipa = ipa });
            }
            else
            {
                missing.Add(word);
            }
        }
        var apiKey = GetApiKey();
        if (missing.Count == 0 || string.IsNullOrWhiteSpace(apiKey)) return results;

        try
        {
            var prompt = $"Return only JSON with an items array. For every English word, items must contain {{\"word\":\"...\",\"ipa\":\"/.../\"}} using US IPA. Preserve the supplied order. Words: {string.Join(", ", missing)}";
            var content = await AskTextModelAsync(apiKey, prompt, cancellationToken);
            using var document = JsonDocument.Parse(StripCodeFence(content));
            if (!document.RootElement.TryGetProperty("items", out var itemsNode) || itemsNode.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            foreach (var item in itemsNode.EnumerateArray())
            {
                var word = item.TryGetProperty("word", out var wordNode) ? NormalizeWord(wordNode.GetString() ?? string.Empty) : string.Empty;
                var ipa = item.TryGetProperty("ipa", out var ipaNode) ? ipaNode.GetString() ?? string.Empty : string.Empty;
                if (word.Length == 0 || ipa.Length == 0) continue;
                SetCachedIpa(word, ipa);
                results.Add(new WordIpaDto { Word = word, Ipa = ipa });
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Batch IPA lookup failed.");
        }

        return results;
    }

    private async Task<string> AskTextModelAsync(
        string apiKey,
        string prompt,
        CancellationToken cancellationToken)
    {
        var model = _configuration["OpenAI:TextModel"] ?? "gpt-4o-mini";
        var payload = new
        {
            model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = "You are a concise English-Vietnamese dictionary and IPA assistant. Return valid JSON only." },
                new { role = "user", content = prompt }
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var client = _httpClientFactory.CreateClient(nameof(OpenAiLanguageReferenceService));
        using var response = await client.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(responseJson);
        return document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? throw new JsonException("OpenAI returned empty content.");
    }

    private string? GetApiKey() => _configuration["OPENAI_API_KEY"] ?? _configuration["OpenAI:ApiKey"];

    private void SetCachedIpa(string word, string ipa) =>
        _cache.Set(GetIpaCacheKey(word), ipa, CacheOptions);

    private static string GetIpaCacheKey(string word) => $"ipa:{word}";

    private static string NormalizeWord(string word) => new(
        word.ToLowerInvariant().Where(character => char.IsLetter(character) || character == '-').ToArray());

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
}
