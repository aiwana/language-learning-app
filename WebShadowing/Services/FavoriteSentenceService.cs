using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class FavoriteSentenceService : IFavoriteSentenceService
{
    private readonly AppDbContext _db;
    public FavoriteSentenceService(AppDbContext db) => _db = db;

    public Task<IReadOnlyList<FavoriteSentenceDto>> ListAsync(long userId, CancellationToken cancellationToken = default) =>
        _db.FavoriteSentences.AsNoTracking().Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new FavoriteSentenceDto(item.FavoriteSentenceId, item.SentenceId,
                item.Sentence.LessonId, item.Sentence.Lesson.Title, item.Sentence.Text,
                item.Sentence.Translation, item.CreatedAt))
            .ToListAsync(cancellationToken).ContinueWith<IReadOnlyList<FavoriteSentenceDto>>(
                task => task.Result, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public async Task<FavoriteSentenceDto?> AddAsync(long userId, long sentenceId, CancellationToken cancellationToken = default)
    {
        var sentence = await _db.LessonSentences.AsNoTracking().Include(item => item.Lesson)
            .SingleOrDefaultAsync(item => item.SentenceId == sentenceId, cancellationToken);
        if (sentence is null) return null;
        var existing = await _db.FavoriteSentences.SingleOrDefaultAsync(
            item => item.UserId == userId && item.SentenceId == sentenceId, cancellationToken);
        if (existing is null)
        {
            existing = new FavoriteSentence { UserId = userId, SentenceId = sentenceId, CreatedAt = DateTime.UtcNow };
            _db.FavoriteSentences.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }
        return new FavoriteSentenceDto(existing.FavoriteSentenceId, sentence.SentenceId, sentence.LessonId,
            sentence.Lesson.Title, sentence.Text, sentence.Translation, existing.CreatedAt);
    }

    public async Task<bool> DeleteAsync(long userId, long favoriteId, CancellationToken cancellationToken = default) =>
        await _db.FavoriteSentences.Where(item => item.UserId == userId && item.FavoriteSentenceId == favoriteId)
            .ExecuteDeleteAsync(cancellationToken) > 0;
}
