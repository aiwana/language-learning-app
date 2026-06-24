using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using WebShadowing.Models;

namespace WebShadowing.Services;

public partial class DictionaryService : IDictionaryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, string> ContractionFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["im"] = "i'm",
        ["ive"] = "i've",
        ["id"] = "i'd",
        ["ill"] = "i'll",
        ["youre"] = "you're",
        ["youve"] = "you've",
        ["youll"] = "you'll",
        ["were"] = "we're",
        ["weve"] = "we've",
        ["theyre"] = "they're",
        ["theyve"] = "they've",
        ["dont"] = "don't",
        ["doesnt"] = "doesn't",
        ["didnt"] = "didn't",
        ["cant"] = "can't",
        ["wont"] = "won't",
        ["isnt"] = "isn't",
        ["arent"] = "aren't",
        ["wasnt"] = "wasn't",
        ["werent"] = "weren't",
        ["hasnt"] = "hasn't",
        ["havent"] = "haven't",
        ["hadnt"] = "hadn't",
        ["wouldnt"] = "wouldn't",
        ["couldnt"] = "couldn't",
        ["shouldnt"] = "shouldn't",
        ["thats"] = "that's",
        ["its"] = "it's",
        ["lets"] = "let's"
    };

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public DictionaryService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<WordLookupResult> LookupWordAsync(string word, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeWord(word);
        if (string.IsNullOrEmpty(normalized))
        {
            return new WordLookupResult { Word = word };
        }

        var map = await LookupWordsAsync([normalized], cancellationToken);
        return map.TryGetValue(normalized, out var result)
            ? result
            : new WordLookupResult { Word = normalized };
    }

    public async Task<IReadOnlyDictionary<string, WordLookupResult>> LookupWordsAsync(
        IEnumerable<string> words,
        CancellationToken cancellationToken = default)
    {
        var distinct = words
            .Select(NormalizeWord)
            .Where(w => w.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new Dictionary<string, WordLookupResult>(StringComparer.OrdinalIgnoreCase);
        var fetchTasks = new List<Task<(string Word, WordLookupResult Result)>>();

        foreach (var word in distinct)
        {
            if (_cache.TryGetValue(CacheKey(word), out WordLookupResult? cached) && cached is not null)
            {
                results[word] = cached;
                continue;
            }

            fetchTasks.Add(FetchAndCacheAsync(word, cancellationToken));
        }

        if (fetchTasks.Count > 0)
        {
            var fetched = await Task.WhenAll(fetchTasks);
            foreach (var (word, result) in fetched)
            {
                results[word] = result;
            }
        }

        return results;
    }

    private async Task<(string Word, WordLookupResult Result)> FetchAndCacheAsync(
        string word,
        CancellationToken cancellationToken)
    {
        var result = await FetchWithVariantsAsync(word, cancellationToken);
        _cache.Set(CacheKey(word), result, TimeSpan.FromHours(24));
        return (word, result);
    }

    private async Task<WordLookupResult> FetchWithVariantsAsync(string word, CancellationToken cancellationToken)
    {
        foreach (var variant in GetLookupVariants(word))
        {
            var result = await FetchFromFreeDictionaryAsync(variant, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.Ipa) || !string.IsNullOrWhiteSpace(result.Meaning))
            {
                return new WordLookupResult
                {
                    Word = word,
                    Ipa = result.Ipa,
                    Meaning = result.Meaning
                };
            }
        }

        return new WordLookupResult { Word = word };
    }

    private static IEnumerable<string> GetLookupVariants(string word)
    {
        yield return word;

        if (ContractionFallbacks.TryGetValue(word, out var contraction))
        {
            yield return contraction;
        }

        if (word.EndsWith("'s", StringComparison.Ordinal) && word.Length > 2)
        {
            yield return word[..^2];
        }
    }

    private async Task<WordLookupResult> FetchFromFreeDictionaryAsync(
        string word,
        CancellationToken cancellationToken)
    {
        var url = $"api/v2/entries/en/{Uri.EscapeDataString(word)}";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new WordLookupResult { Word = word };
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var entries = await JsonSerializer.DeserializeAsync<List<DictionaryEntry>>(stream, JsonOptions, cancellationToken);
            var entry = entries?.FirstOrDefault();
            if (entry is null)
            {
                return new WordLookupResult { Word = word };
            }

            var ipa = entry.Phonetics
                .Select(p => CleanIpa(p.Text))
                .Where(IsUsableIpa)
                .OrderByDescending(t => t.Length)
                .FirstOrDefault()
                ?? string.Empty;

            var meaning = entry.Meanings
                .SelectMany(m => m.Definitions)
                .Select(d => d.Definition)
                .FirstOrDefault(def => !string.IsNullOrWhiteSpace(def))
                ?? string.Empty;

            return new WordLookupResult
            {
                Word = word,
                Ipa = ipa,
                Meaning = meaning
            };
        }
        catch
        {
            return new WordLookupResult { Word = word };
        }
    }

    private static bool IsUsableIpa(string ipa)
    {
        if (string.IsNullOrWhiteSpace(ipa))
        {
            return false;
        }

        var core = ipa.Replace("-", "", StringComparison.Ordinal).Trim('/');
        return core.Length >= 3;
    }

    private static string CacheKey(string word) => $"dict:{word}";

    private static string NormalizeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        var normalized = word.Trim().ToLowerInvariant();
        normalized = TrimEdgePunctuation().Replace(normalized, string.Empty);
        return normalized;
    }

    private static string CleanIpa(string? ipa)
    {
        if (string.IsNullOrWhiteSpace(ipa))
        {
            return string.Empty;
        }

        return ipa.Trim().Trim('/');
    }

    [GeneratedRegex(@"^[^a-z0-9']+|[^a-z0-9']+$", RegexOptions.Compiled)]
    private static partial Regex TrimEdgePunctuation();

    private sealed class DictionaryEntry
    {
        public List<Phonetic> Phonetics { get; set; } = [];
        public List<Meaning> Meanings { get; set; } = [];
    }

    private sealed class Phonetic
    {
        public string? Text { get; set; }
    }

    private sealed class Meaning
    {
        public List<DefinitionEntry> Definitions { get; set; } = [];
    }

    private sealed class DefinitionEntry
    {
        public string? Definition { get; set; }
    }
}
