using System.Text.Json;
// Chức năng: prompt -> nội dung JSON -> TTS từng segment -> draft 24 giờ -> saved lesson.
// Phụ trách chính: Minh Anh. Minh phối hợp cho persistence/ownership.
// Lưu ý: cleanup media và background job TTS vẫn là phần cần hoàn thiện.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class AiLessonGenerationService : IAiLessonGenerationService
{
    private readonly AppDbContext _db;
    private readonly IOpenAiApiClient _openAi;
    private readonly ITtsAudioService _tts;
    private readonly AiLessonOptions _options;
    public AiLessonGenerationService(AppDbContext db, IOpenAiApiClient openAi, ITtsAudioService tts, IOptions<AiLessonOptions> options)
    {
        _db = db; _openAi = openAi; _tts = tts; _options = options.Value;
    }

    public async Task<AiLessonPreviewDto> GenerateAsync(long userId, GenerateAiLessonRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");
        var count = Math.Clamp(request.SentenceCount, 3, _options.MaxSentencesPerLesson);
        var modeInstruction = user.LearningMode switch
        {
            LearningModes.Academic => "Use academic English suitable for presentations, lectures, and research discussion.",
            LearningModes.Professional => "Use professional English suitable for meetings, interviews, and workplace collaboration.",
            _ => "Use natural everyday conversational English."
        };
        var system = "You design short English shadowing lessons for Vietnamese learners. " + modeInstruction
            + " Return valid JSON only with title and segments. Each segment must contain text, translation (Vietnamese), ipa (full sentence IPA), and speaker. Keep every sentence concise and natural.";
        var prompt = $"Topic: {request.Prompt.Trim()}\nCreate exactly {count} connected sentences. Accent: {user.Accent}. JSON shape: {{\"title\":\"...\",\"segments\":[{{\"text\":\"...\",\"translation\":\"...\",\"ipa\":\"/.../\",\"speaker\":\"Narrator\"}}]}}";
        var json = await _openAi.GenerateJsonAsync(_options.GenerationModel, system, prompt, cancellationToken);
        var (title, rawSegments) = ParseLesson(json, count);
        var segments = new List<AiLessonSegmentDto>(rawSegments.Count);
        var scope = $"ai-lesson-{Guid.NewGuid():N}";
        for (var index = 0; index < rawSegments.Count; index++)
        {
            var item = rawSegments[index];
            var audioUrl = await _tts.CreateAsync(item.Text, user.Accent, scope, cancellationToken);
            segments.Add(item with { Order = index, AudioUrl = audioUrl });
        }

        var now = DateTime.UtcNow;
        var preview = new AiLessonPreview
        {
            PreviewId = Guid.NewGuid(), UserId = userId, Prompt = request.Prompt.Trim(), Title = title,
            LearningMode = user.LearningMode, Accent = user.Accent,
            ContentJson = JsonSerializer.Serialize(segments), CreatedAt = now,
            ExpiresAt = now.AddMinutes(_options.PreviewLifetimeMinutes)
        };
        _db.AiLessonPreviews.Add(preview);
        await _db.AiLessonPreviews.Where(item => item.UserId == userId && item.ExpiresAt < now && item.SavedLessonId == null)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var autoSave = await _db.UserSettings.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => item.AutoSaveAiLessons).SingleOrDefaultAsync(cancellationToken);
        if (autoSave)
        {
            var saved = await SaveAsync(userId, preview.PreviewId, cancellationToken);
            return new(preview.PreviewId, title, user.LearningMode, user.Accent, segments, preview.ExpiresAt, true, saved?.SavedLessonId);
        }
        return new(preview.PreviewId, title, user.LearningMode, user.Accent, segments, preview.ExpiresAt);
    }

    public async Task<SavedAiLessonDto?> SaveAsync(long userId, Guid previewId, CancellationToken cancellationToken = default)
    {
        var preview = await _db.AiLessonPreviews.SingleOrDefaultAsync(item => item.PreviewId == previewId && item.UserId == userId, cancellationToken);
        if (preview is null || preview.ExpiresAt < DateTime.UtcNow && preview.SavedLessonId is null) return null;
        if (preview.SavedLessonId is long existingId)
            return await GetSavedAsync(userId, existingId, cancellationToken);
        var segments = JsonSerializer.Deserialize<List<AiLessonSegmentDto>>(preview.ContentJson) ?? [];
        var lesson = new SavedAiLesson
        {
            UserId = userId, Title = preview.Title, LearningMode = preview.LearningMode,
            ContentSnapshot = preview.ContentJson, SourceProvider = "openai", SourceId = preview.PreviewId.ToString(),
            SourceReviewStatus = SourceReviewStatuses.Approved, SourceReviewedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            Segments = segments.Select(item => new SavedAiLessonSegment
            {
                SegmentOrder = item.Order, Text = item.Text, Translation = item.Translation,
                Ipa = item.Ipa, AudioUrl = item.AudioUrl, Speaker = item.Speaker
            }).ToList()
        };
        _db.SavedAiLessons.Add(lesson);
        await _db.SaveChangesAsync(cancellationToken);
        preview.SavedLessonId = lesson.SavedLessonId;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(lesson);
    }

    public async Task<AiLessonPreviewDto?> GetPreviewAsync(long userId, Guid previewId, CancellationToken cancellationToken = default)
    {
        var preview = await _db.AiLessonPreviews.AsNoTracking()
            .SingleOrDefaultAsync(item => item.PreviewId == previewId && item.UserId == userId, cancellationToken);
        if (preview is null || preview.ExpiresAt < DateTime.UtcNow && preview.SavedLessonId is null) return null;
        var segments = JsonSerializer.Deserialize<List<AiLessonSegmentDto>>(preview.ContentJson) ?? [];
        return new(preview.PreviewId, preview.Title, preview.LearningMode, preview.Accent, segments,
            AsUtc(preview.ExpiresAt), preview.SavedLessonId.HasValue, preview.SavedLessonId);
    }

    public async Task<IReadOnlyList<AiLessonPreviewDto>> ListPreviewsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        // Normalize previews created while the old 30-minute policy was active.
        // This keeps the lifetime rule consistently based on CreatedAt, including
        // drafts that already existed when the configuration changed to 24 hours.
        await _db.AiLessonPreviews
            .Where(item => item.UserId == userId
                && item.SavedLessonId == null
                && item.ExpiresAt < item.CreatedAt.AddMinutes(_options.PreviewLifetimeMinutes)
                && item.CreatedAt.AddMinutes(_options.PreviewLifetimeMinutes) >= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.ExpiresAt,
                    item => item.CreatedAt.AddMinutes(_options.PreviewLifetimeMinutes)),
                cancellationToken);
        await _db.AiLessonPreviews
            .Where(item => item.UserId == userId && item.ExpiresAt < now && item.SavedLessonId == null)
            .ExecuteDeleteAsync(cancellationToken);
        var previews = await _db.AiLessonPreviews.AsNoTracking()
            .Where(item => item.UserId == userId && item.SavedLessonId == null && item.ExpiresAt >= now)
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        return previews.Select(item => new AiLessonPreviewDto(
            item.PreviewId,
            item.Title,
            item.LearningMode,
            item.Accent,
            JsonSerializer.Deserialize<List<AiLessonSegmentDto>>(item.ContentJson) ?? [],
            AsUtc(item.ExpiresAt))).ToList();
    }

    public async Task<IReadOnlyList<SavedAiLessonDto>> ListAsync(long userId, CancellationToken cancellationToken = default)
    {
        var lessons = await _db.SavedAiLessons.AsNoTracking().Include(item => item.Segments)
            .Where(item => item.UserId == userId).OrderByDescending(item => item.UpdatedAt).Take(50).ToListAsync(cancellationToken);
        return lessons.Select(ToDto).ToList();
    }

    public async Task<bool> DeleteAsync(long userId, long savedLessonId, CancellationToken cancellationToken = default)
    {
        await _db.AiLessonPreviews.Where(item => item.UserId == userId && item.SavedLessonId == savedLessonId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.SavedLessonId, (long?)null), cancellationToken);
        return await _db.SavedAiLessons.Where(item => item.UserId == userId && item.SavedLessonId == savedLessonId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public async Task<SavedAiLessonDto?> GetSavedAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var item = await _db.SavedAiLessons.AsNoTracking().Include(lesson => lesson.Segments)
            .SingleOrDefaultAsync(lesson => lesson.UserId == userId && lesson.SavedLessonId == id, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<bool> DeletePreviewAsync(long userId, Guid previewId, CancellationToken cancellationToken = default) =>
        await _db.AiLessonPreviews
            .Where(item => item.UserId == userId && item.PreviewId == previewId && item.SavedLessonId == null)
            .ExecuteDeleteAsync(cancellationToken) > 0;

    private static SavedAiLessonDto ToDto(SavedAiLesson lesson) => new(lesson.SavedLessonId, lesson.Title, lesson.LearningMode, lesson.UpdatedAt,
        lesson.Segments.OrderBy(item => item.SegmentOrder).Select(item => new AiLessonSegmentDto(item.SegmentOrder, item.Text,
            item.Translation ?? string.Empty, item.Ipa ?? string.Empty, item.AudioUrl, item.Speaker, item.SavedSegmentId)).ToList());

    private static (string Title, List<AiLessonSegmentDto> Segments) ParseLesson(string json, int maxCount)
    {
        using var document = JsonDocument.Parse(StripFence(json));
        var root = document.RootElement;
        var title = root.TryGetProperty("title", out var titleNode) ? titleNode.GetString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(title)) title = "Bài học AI";
        if (!root.TryGetProperty("segments", out var segmentsNode) || segmentsNode.ValueKind != JsonValueKind.Array)
            throw new OpenAiServiceUnavailableException("AI trả về bài học không đúng cấu trúc.");
        var segments = new List<AiLessonSegmentDto>();
        foreach (var node in segmentsNode.EnumerateArray().Take(maxCount))
        {
            var text = node.TryGetProperty("text", out var textNode) ? textNode.GetString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(text)) continue;
            segments.Add(new(segments.Count, text,
                node.TryGetProperty("translation", out var translation) ? translation.GetString()?.Trim() ?? string.Empty : string.Empty,
                node.TryGetProperty("ipa", out var ipa) ? ipa.GetString()?.Trim() ?? string.Empty : string.Empty,
                null,
                node.TryGetProperty("speaker", out var speaker) ? speaker.GetString()?.Trim() : null));
        }
        if (segments.Count < 3) throw new OpenAiServiceUnavailableException("AI chưa tạo đủ câu cho bài học.");
        return (title[..Math.Min(title.Length, 255)], segments);
    }

    private static string StripFence(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal)) return trimmed;
        var firstBreak = trimmed.IndexOf('\n');
        var last = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstBreak >= 0 && last > firstBreak ? trimmed[(firstBreak + 1)..last].Trim() : trimmed;
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
