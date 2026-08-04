using System.Text.Json;
// Chức năng: đọc câu/timeline từ DB hoặc transcript JSON mà không tự ý ghi dữ liệu import vào DB.
// Phụ trách nội dung/transcript: Hải Anh. Minh review mapping và tính toàn vẹn dữ liệu.
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class LessonContentService : ILessonContentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public LessonContentService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IReadOnlyList<LessonSentenceDto>> GetSentencesAsync(
        long lessonId,
        IReadOnlyCollection<LessonMaterial> materials,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<LessonSentence>? preloadedDbSentences = null)
    {
        var transcriptSentences = await LoadTranscriptSentencesAsync(materials, cancellationToken);

        var dbSentences = preloadedDbSentences is null
            ? await _db.LessonSentences
                .AsNoTracking()
                .Where(sentence => sentence.LessonId == lessonId)
                .OrderBy(sentence => sentence.SentenceOrder)
                .Select(sentence => new LessonSentenceDto
                {
                    SentenceId = sentence.SentenceId,
                    Order = sentence.SentenceOrder,
                    Text = sentence.Text,
                    Translation = sentence.Translation,
                    Ipa = sentence.Ipa
                })
                .ToListAsync(cancellationToken)
            : preloadedDbSentences
                .OrderBy(sentence => sentence.SentenceOrder)
                .Select(MapDbSentence)
                .ToList();

        if (transcriptSentences.Count > 0)
        {
            return MergeTranscriptWithDbSentences(transcriptSentences, dbSentences);
        }

        return dbSentences;
    }

    private static LessonSentenceDto MapDbSentence(LessonSentence sentence) => new()
    {
        SentenceId = sentence.SentenceId,
        Order = sentence.SentenceOrder,
        Text = sentence.Text,
        Translation = sentence.Translation,
        Ipa = sentence.Ipa
    };

    public async Task<bool> HasSentencesAsync(
        long lessonId,
        IReadOnlyCollection<LessonMaterial> materials,
        CancellationToken cancellationToken = default)
    {
        var hasDbSentences = await _db.LessonSentences
            .AsNoTracking()
            .AnyAsync(sentence => sentence.LessonId == lessonId, cancellationToken);

        if (hasDbSentences)
        {
            return true;
        }

        var fallbackSentences = await LoadTranscriptSentencesAsync(materials, cancellationToken);
        return fallbackSentences.Count > 0;
    }

    public async Task<bool> HasTranscriptAsync(
        IReadOnlyCollection<LessonMaterial> materials,
        CancellationToken cancellationToken = default)
    {
        var sentences = await LoadTranscriptSentencesAsync(materials, cancellationToken);
        return sentences.Count > 0;
    }

    private async Task<IReadOnlyList<LessonSentenceDto>> LoadTranscriptSentencesAsync(
        IReadOnlyCollection<LessonMaterial> materials,
        CancellationToken cancellationToken)
    {
        var transcriptUrl = materials
            .Where(material => material.MaterialType == MaterialTypes.Transcript)
            .Select(material => material.ContentUrl)
            .FirstOrDefault();

        var path = ResolveTranscriptPath(transcriptUrl);
        if (path is null || !File.Exists(path))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(path);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var sentencesElement = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement
                : document.RootElement.TryGetProperty("sentences", out var nestedSentences)
                    ? nestedSentences
                    : default;

            if (sentencesElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var sentences = new List<LessonSentenceDto>();
            foreach (var item in sentencesElement.EnumerateArray())
            {
                var text = GetString(item, "text");
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                sentences.Add(new LessonSentenceDto
                {
                    SentenceId = 0,
                    Order = GetInt32(item, "sentence_order") ?? GetInt32(item, "order") ?? sentences.Count + 1,
                    Text = text,
                    Translation = GetString(item, "translation"),
                    Ipa = GetString(item, "ipa"),
                    StartTime = GetDouble(item, "start_time") ?? GetDouble(item, "startTime"),
                    EndTime = GetDouble(item, "end_time") ?? GetDouble(item, "endTime")
                });
            }

            return sentences
                .OrderBy(sentence => sentence.Order)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IReadOnlyList<LessonSentenceDto> MergeTranscriptWithDbSentences(
        IReadOnlyList<LessonSentenceDto> transcriptSentences,
        IReadOnlyList<LessonSentenceDto> dbSentences)
    {
        if (dbSentences.Count == 0)
        {
            return transcriptSentences;
        }

        var dbByText = dbSentences
            .GroupBy(sentence => NormalizeSentenceKey(sentence.Text))
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(group => group.Key, group => group.First());

        var dbByOrder = dbSentences
            .GroupBy(sentence => sentence.Order)
            .ToDictionary(group => group.Key, group => group.First());

        return transcriptSentences
            .Select(transcript =>
            {
                dbByText.TryGetValue(NormalizeSentenceKey(transcript.Text), out var dbMatch);

                if (dbMatch is null
                    && dbByOrder.TryGetValue(transcript.Order, out var orderMatch)
                    && NormalizeSentenceKey(orderMatch.Text) == NormalizeSentenceKey(transcript.Text))
                {
                    dbMatch = orderMatch;
                }

                return new LessonSentenceDto
                {
                    SentenceId = dbMatch?.SentenceId ?? 0,
                    Order = transcript.Order,
                    Text = transcript.Text,
                    Translation = transcript.Translation ?? dbMatch?.Translation,
                    Ipa = transcript.Ipa ?? dbMatch?.Ipa,
                    StartTime = transcript.StartTime,
                    EndTime = transcript.EndTime
                };
            })
            .OrderBy(sentence => sentence.Order)
            .ToList();
    }

    private string? ResolveTranscriptPath(string? contentUrl)
    {
        var path = ResolveWebRootPath(contentUrl);
        if (path is null || File.Exists(path))
        {
            return path;
        }

        if (Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            var jsonPath = Path.ChangeExtension(path, ".json");
            if (File.Exists(jsonPath))
            {
                return jsonPath;
            }
        }

        return path;
    }

    private string? ResolveWebRootPath(string? contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl) || !contentUrl.StartsWith('/'))
        {
            return null;
        }

        var relativePath = contentUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_env.WebRootPath, relativePath);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? GetInt32(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.TryGetDouble(out var value) ? value : null;
    }

    private static string NormalizeSentenceKey(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return new string(text
            .ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character))
            .ToArray())
            .Trim();
    }
}
