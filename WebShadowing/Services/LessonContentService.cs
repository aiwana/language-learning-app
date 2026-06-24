using System.Text.Json;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class LessonContentService : ILessonContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _env;

    public LessonContentService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string? ResolveWebRootPath(string? contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl) || !contentUrl.StartsWith('/'))
        {
            return null;
        }

        var relative = contentUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_env.WebRootPath, relative);
    }

    public async Task<IReadOnlyList<LessonSentenceViewModel>> LoadSentencesAsync(
        string? contentUrl,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveWebRootPath(contentUrl);
        if (path is null || !File.Exists(path))
        {
            return Array.Empty<LessonSentenceViewModel>();
        }

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<TranscriptDocument>(stream, JsonOptions, cancellationToken);
        if (document?.Sentences is not { Count: > 0 })
        {
            return Array.Empty<LessonSentenceViewModel>();
        }

        return document.Sentences;
    }

    public async Task SaveTranscriptAsync(
        string contentUrl,
        IReadOnlyList<LessonSentenceViewModel> sentences,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveWebRootPath(contentUrl)
            ?? throw new InvalidOperationException($"Invalid content path: {contentUrl}");

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var payload = new { sentences };
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken);
    }

    private sealed class TranscriptDocument
    {
        public List<LessonSentenceViewModel> Sentences { get; set; } = [];
    }
}
