using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class VocabularyNotebookService : IVocabularyNotebookService
{
    private static readonly Regex NormalizeRegex = new("[^\\p{L}\\p{Nd}']+", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly ILanguageReferenceService _languageReferenceService;
    private readonly IUserContextService _userContextService;
    private readonly VocabularyOptions _options;

    public VocabularyNotebookService(
        AppDbContext db,
        ILanguageReferenceService languageReferenceService,
        IUserContextService userContextService,
        IOptions<VocabularyOptions> options)
    {
        _db = db;
        _languageReferenceService = languageReferenceService;
        _userContextService = userContextService;
        _options = options.Value;
    }

    public async Task<VocabularyPageDto> GetPageAsync(
        long userId,
        int page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize ?? _options.DefaultPageSize, 1, 100);

        var query = _db.VocabularyItems
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return new VocabularyPageDto(
            items.Select(MapVocabularyItem).ToList(),
            total,
            safePage,
            safePageSize);
    }

    public async Task<VocabularyItemDto> UpsertAsync(
        long userId,
        AddVocabularyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedWord = NormalizeWord(request.Word)
            ?? throw new InvalidOperationException("Từ cần lưu không hợp lệ.");
        var sourceSentence = request.SourceSentenceId is null
            ? null
            : await _db.LessonSentences
                .AsNoTracking()
                .Include(item => item.Lesson)
                    .ThenInclude(item => item.Course)
                .SingleOrDefaultAsync(item => item.SentenceId == request.SourceSentenceId.Value, cancellationToken);

        var accent = await _userContextService.GetAccentAsync(cancellationToken);
        var context = FirstNonEmpty(request.ExampleSentence, sourceSentence?.Text);
        var meaning = await _languageReferenceService.GetMeaningAsync(request.Word.Trim(), context, cancellationToken);
        var ipaBatch = await _languageReferenceService.GetIpaBatchAsync([request.Word.Trim()], accent, cancellationToken);

        var item = await _db.VocabularyItems.SingleOrDefaultAsync(
            entry => entry.UserId == userId
                && entry.NormalizedWord == normalizedWord
                && entry.LanguageCode == "en",
            cancellationToken);
        if (item is null)
        {
            item = new VocabularyItem
            {
                UserId = userId,
                NormalizedWord = normalizedWord,
                LanguageCode = "en",
                CreatedAt = DateTime.UtcNow
            };
            _db.VocabularyItems.Add(item);
        }

        item.DisplayWord = request.Word.Trim();
        item.Ipa = FirstNonEmpty(request.Ipa, meaning.Ipa, ipaBatch.FirstOrDefault()?.Ipa, item.Ipa);
        item.Meaning = FirstNonEmpty(request.Meaning, meaning.Meaning, item.Meaning);
        item.ExampleSentence = FirstNonEmpty(request.ExampleSentence, sourceSentence?.Text, item.ExampleSentence);
        item.SourceSentenceId = sourceSentence?.SentenceId;
        item.SourceType = sourceSentence is null ? VocabularySourceTypes.AiSnapshot : VocabularySourceTypes.LessonSentence;
        item.SourceLessonId = sourceSentence?.LessonId;
        item.SourceLessonTitle = sourceSentence?.Lesson.Title;
        item.SourceSentenceText = FirstNonEmpty(sourceSentence?.Text, request.ExampleSentence, item.SourceSentenceText);
        item.SourceLearningMode = FirstNonEmpty(sourceSentence?.Lesson.Course.LearningMode, item.SourceLearningMode);
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return MapVocabularyItem(item);
    }

    public async Task<bool> DeleteAsync(
        long userId,
        long vocabularyItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.VocabularyItems.SingleOrDefaultAsync(
            entry => entry.UserId == userId && entry.VocabularyItemId == vocabularyItemId,
            cancellationToken);
        if (item is null)
        {
            return false;
        }

        _db.VocabularyItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static VocabularyItemDto MapVocabularyItem(VocabularyItem item) => new(
        item.VocabularyItemId,
        item.DisplayWord,
        item.Ipa,
        item.Meaning,
        item.ExampleSentence,
        item.ReviewStatus,
        item.LastReviewedAt,
        item.ReviewCount,
        item.SourceSentenceId,
        new VocabularySourceContextDto(
            item.SourceType,
            item.SourceLessonId,
            item.SourceLessonTitle,
            item.SourceSentenceId,
            item.SourceSentenceText,
            item.SourceLearningMode,
            item.CreatedAt));

    private static string? NormalizeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        var normalized = NormalizeRegex.Replace(word.Trim().ToLowerInvariant(), string.Empty);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}