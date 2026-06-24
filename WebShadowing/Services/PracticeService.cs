using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class PracticeService : IPracticeService
{
    private readonly AppDbContext _db;

    public PracticeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PracticeSessionResult> StartSessionAsync(
        long userId,
        long lessonId,
        CancellationToken cancellationToken = default)
    {
        var lessonExists = await _db.Lessons
            .AsNoTracking()
            .AnyAsync(l => l.LessonId == lessonId, cancellationToken);

        if (!lessonExists)
        {
            return new PracticeSessionResult(false, ErrorMessage: "Bài học không tồn tại.");
        }

        var session = new PracticeSession
        {
            UserId = userId,
            LessonId = lessonId,
            StartedAt = DateTime.UtcNow
        };

        _db.PracticeSessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);

        return new PracticeSessionResult(true, session.SessionId);
    }

    public async Task<PracticeSessionResult> CompleteSessionAsync(
        long userId,
        long sessionId,
        decimal overallScore,
        CancellationToken cancellationToken = default)
    {
        if (overallScore is < 0 or > 100)
        {
            return new PracticeSessionResult(false, ErrorMessage: "Điểm phải nằm trong khoảng 0–100.");
        }

        var session = await _db.PracticeSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId, cancellationToken);

        if (session is null)
        {
            return new PracticeSessionResult(false, ErrorMessage: "Không tìm thấy phiên luyện tập.");
        }

        if (session.CompletedAt is not null)
        {
            return new PracticeSessionResult(false, ErrorMessage: "Phiên luyện tập đã được hoàn thành.");
        }

        var completedAt = DateTime.UtcNow;
        session.CompletedAt = completedAt;
        session.OverallScore = overallScore;

        await UpdateUserStatisticsAsync(userId, overallScore, completedAt, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new PracticeSessionResult(true, session.SessionId);
    }

    private async Task UpdateUserStatisticsAsync(
        long userId,
        decimal overallScore,
        DateTime completedAt,
        CancellationToken cancellationToken)
    {
        var stats = await _db.UserStatistics
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (stats is null)
        {
            stats = new UserStatistic
            {
                UserId = userId,
                TotalSessions = 0,
                AverageScore = 0,
                StreakDays = 0
            };
            _db.UserStatistics.Add(stats);
        }

        var previousTotal = stats.TotalSessions;
        stats.TotalSessions = previousTotal + 1;
        stats.AverageScore = previousTotal == 0
            ? overallScore
            : Math.Round(
                ((stats.AverageScore * previousTotal) + overallScore) / stats.TotalSessions,
                2,
                MidpointRounding.AwayFromZero);

        stats.StreakDays = CalculateStreak(stats.LastPracticeAt, completedAt, stats.StreakDays);
        stats.LastPracticeAt = completedAt;
    }

    private static int CalculateStreak(DateTime? lastPracticeAt, DateTime completedAt, int currentStreak)
    {
        if (lastPracticeAt is null)
        {
            return 1;
        }

        var lastDate = lastPracticeAt.Value.Date;
        var today = completedAt.Date;

        if (lastDate == today)
        {
            return Math.Max(currentStreak, 1);
        }

        if (lastDate == today.AddDays(-1))
        {
            return currentStreak + 1;
        }

        return 1;
    }
}
