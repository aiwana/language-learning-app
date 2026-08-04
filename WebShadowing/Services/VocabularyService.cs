using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class VocabularyService : IVocabularyService
{
    private readonly AppDbContext _db;
    private readonly ILanguageReferenceService _languageReference;
    private readonly VocabularyOptions _options;

    public VocabularyService(AppDbContext db, ILanguageReferenceService languageReference, IOptions<VocabularyOptions> options)
    {
        _db = db;
        _languageReference = languageReference;
        _options = options.Value;
    }

    public async Task<VocabularyPageDto> GetPageAsync(long userId, string? status, int page, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var size = Math.Clamp(pageSize ?? _options.DefaultPageSize, 1, 100);
        var normalizedStatus = status?.Trim().ToLowerInvariant();
        var query = _db.VocabularyItems.AsNoTracking().Where(item => item.UserId == userId);
        if (normalizedStatus is VocabularyReviewStatuses.Active or VocabularyReviewStatuses.Mastered)
        {
            query = query.Where(item => item.ReviewStatus == normalizedStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.ReviewStatus).ThenByDescending(item => item.UpdatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(item => ToDto(item)).ToListAsync(cancellationToken);
        return new VocabularyPageDto(items, total, page, size);
    }

    public Task<VocabularyItemDto?> GetAsync(long userId, long itemId, CancellationToken cancellationToken = default) =>
        _db.VocabularyItems.AsNoTracking()
            .Where(item => item.UserId == userId && item.VocabularyItemId == itemId)
            .Select(item => ToDto(item)).SingleOrDefaultAsync(cancellationToken);

    public async Task<VocabularyItemDto> AddAsync(long userId, AddVocabularyRequestDto request, CancellationToken cancellationToken = default)
    {
        var word = request.Word.Trim();
        var normalized = NormalizeWord(word);
        if (normalized.Length == 0) throw new ArgumentException("Từ vựng không hợp lệ.", nameof(request));

        var existing = await _db.VocabularyItems.SingleOrDefaultAsync(
            item => item.UserId == userId && item.NormalizedWord == normalized && item.LanguageCode == "en",
            cancellationToken);
        if (existing is not null)
        {
            existing.ReviewStatus = VocabularyReviewStatuses.Active;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Ipa = string.IsNullOrWhiteSpace(request.Ipa) ? existing.Ipa : request.Ipa.Trim();
            existing.Meaning = string.IsNullOrWhiteSpace(request.Meaning) ? existing.Meaning : request.Meaning.Trim();
            existing.ExampleSentence ??= request.ExampleSentence?.Trim();
            existing.SourceSentenceId ??= request.SourceSentenceId;
            await _db.SaveChangesAsync(cancellationToken);
            return ToDto(existing);
        }

        var ipa = request.Ipa?.Trim();
        var meaning = request.Meaning?.Trim();
        if (string.IsNullOrWhiteSpace(ipa) || string.IsNullOrWhiteSpace(meaning))
        {
            var reference = await _languageReference.GetMeaningAsync(word, request.ExampleSentence, cancellationToken);
            ipa = string.IsNullOrWhiteSpace(ipa) ? reference.Ipa : ipa;
            meaning = string.IsNullOrWhiteSpace(meaning) ? reference.Meaning : meaning;
        }

        var item = new VocabularyItem
        {
            UserId = userId,
            NormalizedWord = normalized,
            DisplayWord = word,
            Ipa = ipa,
            Meaning = meaning,
            ExampleSentence = request.ExampleSentence?.Trim(),
            SourceSentenceId = request.SourceSentenceId,
            ReviewStatus = VocabularyReviewStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.VocabularyItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public Task<bool> MarkMasteredAsync(long userId, long itemId, CancellationToken cancellationToken = default) =>
        UpdateReviewAsync(userId, itemId, VocabularyReviewStatuses.Mastered, cancellationToken);

    public Task<bool> ResetReviewAsync(long userId, long itemId, CancellationToken cancellationToken = default) =>
        UpdateReviewAsync(userId, itemId, VocabularyReviewStatuses.Active, cancellationToken);

    public async Task<bool> DeleteAsync(long userId, long itemId, CancellationToken cancellationToken = default)
    {
        var affected = await _db.VocabularyItems
            .Where(item => item.UserId == userId && item.VocabularyItemId == itemId)
            .ExecuteDeleteAsync(cancellationToken);
        return affected > 0;
    }

    private async Task<bool> UpdateReviewAsync(long userId, long itemId, string status, CancellationToken cancellationToken)
    {
        var item = await _db.VocabularyItems.SingleOrDefaultAsync(
            entry => entry.UserId == userId && entry.VocabularyItemId == itemId,
            cancellationToken);
        if (item is null) return false;
        item.ReviewStatus = status;
        item.LastReviewedAt = DateTime.UtcNow;
        item.ReviewCount++;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static VocabularyItemDto ToDto(VocabularyItem item) => new(
        item.VocabularyItemId, item.DisplayWord, item.Ipa, item.Meaning, item.ExampleSentence,
        item.ReviewStatus, item.LastReviewedAt, item.ReviewCount, item.SourceSentenceId, SourceContext: null);

    internal static string NormalizeWord(string value) => new(
        value.Trim().ToLowerInvariant().Where(character => char.IsLetter(character) || character == '-').ToArray());
}
