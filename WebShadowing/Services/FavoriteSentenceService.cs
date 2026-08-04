using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class FavoriteSentenceService : IFavoriteSentenceService
{
    private readonly AppDbContext _db;

    public FavoriteSentenceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FavoriteSentenceDto>> GetListAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var items = await _db.FavoriteSentences
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(MapFavoriteSentence).ToList();
    }

    public async Task<FavoriteSentenceStatusDto> GetStatusAsync(
        long userId,
        FavoriteSentenceStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var lookup = await ResolveLookupAsync(userId, request.SentenceId, request.SavedSegmentId, request.Text, request.Translation, request.LearningMode, cancellationToken);
        var item = await _db.FavoriteSentences
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.UserId == userId
                && entry.SourceType == lookup.SourceType
                && entry.SourceKey == lookup.SourceKey,
                cancellationToken);

        return new FavoriteSentenceStatusDto(
            item is not null,
            item?.FavoriteSentenceId,
            lookup.SourceType,
            lookup.SourceKey);
    }

    public async Task<FavoriteSentenceMutationDto> SaveAsync(
        long userId,
        AddFavoriteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var lookup = await ResolveLookupAsync(userId, request.SentenceId, request.SavedSegmentId, request.Text, request.Translation, request.LearningMode, cancellationToken, request.LessonTitle);
        var existing = await _db.FavoriteSentences.SingleOrDefaultAsync(
            entry => entry.UserId == userId
                && entry.SourceType == lookup.SourceType
                && entry.SourceKey == lookup.SourceKey,
            cancellationToken);

        if (existing is not null)
        {
            return new FavoriteSentenceMutationDto(true, true, MapFavoriteSentence(existing));
        }

        var favorite = new FavoriteSentence
        {
            UserId = userId,
            SentenceId = lookup.SentenceId,
            SavedSegmentId = lookup.SavedSegmentId,
            SourceType = lookup.SourceType,
            SourceKey = lookup.SourceKey,
            LessonId = lookup.LessonId,
            LessonTitle = lookup.LessonTitle,
            TextSnapshot = lookup.Text,
            TranslationSnapshot = lookup.Translation,
            LearningMode = lookup.LearningMode,
            CreatedAt = DateTime.UtcNow
        };

        _db.FavoriteSentences.Add(favorite);
        await _db.SaveChangesAsync(cancellationToken);
        return new FavoriteSentenceMutationDto(true, false, MapFavoriteSentence(favorite));
    }

    public async Task<bool> DeleteAsync(
        long userId,
        long favoriteSentenceId,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.FavoriteSentences.SingleOrDefaultAsync(
            entry => entry.UserId == userId && entry.FavoriteSentenceId == favoriteSentenceId,
            cancellationToken);
        if (item is null)
        {
            return false;
        }

        _db.FavoriteSentences.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<ResolvedFavoriteLookup> ResolveLookupAsync(
        long userId,
        long? sentenceId,
        long? savedSegmentId,
        string? text,
        string? translation,
        string? learningMode,
        CancellationToken cancellationToken,
        string? lessonTitle = null)
    {
        if (sentenceId is not null)
        {
            var sentence = await _db.LessonSentences
                .AsNoTracking()
                .Include(item => item.Lesson)
                .SingleOrDefaultAsync(item => item.SentenceId == sentenceId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy câu học để lưu yêu thích.");

            return new ResolvedFavoriteLookup(
                FavoriteSourceTypes.LessonSentence,
                $"sentence:{sentence.SentenceId}",
                sentence.SentenceId,
                null,
                sentence.LessonId,
                sentence.Lesson.Title,
                sentence.Text,
                sentence.Translation,
                learningMode);
        }

        if (savedSegmentId is not null)
        {
            var segment = await _db.SavedAiLessonSegments
                .AsNoTracking()
                .Include(item => item.SavedLesson)
                .SingleOrDefaultAsync(
                    item => item.SavedSegmentId == savedSegmentId.Value && item.SavedLesson.UserId == userId,
                    cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy đoạn bài AI để lưu yêu thích.");

            return new ResolvedFavoriteLookup(
                FavoriteSourceTypes.AiSnapshot,
                ComputeContentHash(segment.Text, segment.Translation, segment.SavedLesson.LearningMode),
                null,
                segment.SavedSegmentId,
                null,
                segment.SavedLesson.Title,
                segment.Text,
                segment.Translation,
                segment.SavedLesson.LearningMode);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Thiếu nội dung câu để lưu yêu thích.");
        }

        var effectiveLearningMode = string.IsNullOrWhiteSpace(learningMode)
            ? null
            : learningMode.Trim().ToLowerInvariant();

        return new ResolvedFavoriteLookup(
            FavoriteSourceTypes.AiSnapshot,
            ComputeContentHash(text, translation, effectiveLearningMode),
            null,
            null,
            null,
            lessonTitle,
            text.Trim(),
            string.IsNullOrWhiteSpace(translation) ? null : translation.Trim(),
            effectiveLearningMode);
    }

    private static FavoriteSentenceDto MapFavoriteSentence(FavoriteSentence item) => new(
        item.FavoriteSentenceId,
        item.SentenceId ?? 0,
        item.LessonId ?? 0,
        item.LessonTitle ?? string.Empty,
        item.TextSnapshot,
        item.TranslationSnapshot,
        item.CreatedAt,
        item.SourceType,
        item.SourceKey,
        new FavoriteSentenceContextDto(
            item.SourceType,
            item.SourceKey,
            item.LessonId,
            item.LessonTitle,
            item.SentenceId,
            item.SavedSegmentId,
            item.TextSnapshot,
            item.TranslationSnapshot,
            item.LearningMode,
            item.CreatedAt));

    private static string ComputeContentHash(string text, string? translation, string? learningMode)
    {
        var content = string.Join("\n", text.Trim(), translation?.Trim() ?? string.Empty, learningMode?.Trim().ToLowerInvariant() ?? string.Empty);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record ResolvedFavoriteLookup(
        string SourceType,
        string SourceKey,
        long? SentenceId,
        long? SavedSegmentId,
        long? LessonId,
        string? LessonTitle,
        string Text,
        string? Translation,
        string? LearningMode);
}