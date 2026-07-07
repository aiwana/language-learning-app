using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class LessonContentService : ILessonContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        CancellationToken cancellationToken = default)
    {
        var dbSentences = await _db.LessonSentences
            .AsNoTracking()
            .Where(sentence => sentence.LessonId == lessonId)
            .OrderBy(sentence => sentence.SentenceOrder)
            .Select(sentence => new LessonSentenceDto
            {
                SentenceId = sentence.SentenceId,
                Order = sentence.SentenceOrder,
                Text = sentence.Text,
                Translation = sentence.Translation
            })
            .ToListAsync(cancellationToken);

        if (dbSentences.Count > 0)
        {
            return dbSentences;
        }

        return await LoadTranscriptSentencesAsync(materials, cancellationToken);
    }

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

        var path = ResolveWebRootPath(transcriptUrl);
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
                    Translation = GetString(item, "translation")
                });
            }

            return sentences
                .OrderBy(sentence => sentence.Order)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Transcript file is corrupt, locked, or unreadable.
            // Per spec: fallback errors return [] so hasContent stays false — do NOT 500.
            return [];
        }
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
}
