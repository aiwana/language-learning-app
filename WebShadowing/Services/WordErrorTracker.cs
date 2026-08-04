using Microsoft.EntityFrameworkCore;
// Chức năng: theo dõi số lần sai liên tiếp theo từ và đưa từ đạt ngưỡng vào sổ từ vựng.
// Phụ trách chính: Minh. Dữ liệu này được Hải Anh hiển thị/kiểm thử trên trang Tiến trình.
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class WordErrorTracker : IWordErrorTracker
{
    private readonly AppDbContext _db;
    private readonly IVocabularyService _vocabulary;
    private readonly VocabularyOptions _options;
    public WordErrorTracker(AppDbContext db, IVocabularyService vocabulary, IOptions<VocabularyOptions> options)
    {
        _db = db;
        _vocabulary = vocabulary;
        _options = options.Value;
    }

    public async Task TrackAsync(long userId, LessonSentenceDto sentence, IReadOnlyList<WordFeedbackDto> feedback, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var words = sentence.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < words.Length; index++)
        {
            var normalized = VocabularyService.NormalizeWord(words[index]);
            if (normalized.Length == 0) continue;
            var item = feedback.ElementAtOrDefault(index);
            var isError = string.Equals(item?.AccuracyCode, "incorrect", StringComparison.OrdinalIgnoreCase);
            var stat = await _db.WordErrorStatistics.SingleOrDefaultAsync(
                entry => entry.UserId == userId && entry.NormalizedWord == normalized, cancellationToken);
            if (stat is null && !isError) continue;
            stat ??= new WordErrorStatistic
            {
                UserId = userId, NormalizedWord = normalized, DisplayWord = words[index].Trim(),
                LastSentenceId = sentence.SentenceId
            };
            if (_db.Entry(stat).State == EntityState.Detached) _db.WordErrorStatistics.Add(stat);
            stat.LastAttemptedAt = now;
            stat.UpdatedAt = now;
            if (isError)
            {
                stat.ConsecutiveErrorCount++;
                stat.TotalErrorCount++;
                stat.LastErrorAt = now;
                stat.LastSentenceId = sentence.SentenceId;
            }
            else
            {
                stat.ConsecutiveErrorCount = 0;
            }

            if (isError && stat.ConsecutiveErrorCount >= _options.WordErrorThreshold)
            {
                await _db.SaveChangesAsync(cancellationToken);
                await _vocabulary.AddAsync(userId, new AddVocabularyRequestDto
                {
                    Word = words[index], ExampleSentence = sentence.Text, SourceSentenceId = sentence.SentenceId
                }, cancellationToken);
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
