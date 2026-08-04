using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.AuthFlowTests;

public sealed class GamificationServiceIntegrationTests
{
    [ConfiguredSqlServerFact]
    public async Task FourthConsecutiveFailure_AutoSavesVocabularyOnce_AndCorrectAttemptResetsStreak()
    {
        using var factory = new AuthFlowApplicationFactory(configureServices: services =>
        {
            services.RemoveAll<ILanguageReferenceService>();
            services.AddSingleton<ILanguageReferenceService>(new StubLanguageReferenceService());
        });
        var seeded = await SeedAsync(factory.Services);

        for (var index = 1; index <= 4; index++)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IGamificationService>();
            var result = await service.ProcessVerifiedAttemptAsync(new VerifiedPracticeAttempt
            {
                UserId = seeded.UserId,
                LessonId = seeded.LessonId,
                SentenceId = seeded.SentenceId,
                PracticeTab = PracticeTabs.Shadowing,
                ExerciseType = ExerciseTypes.Pronunciation,
                TargetScore = 70,
                Score = 40,
                Passed = false,
                IdempotencyKey = $"autosave-{index}",
                AssessmentProvider = "integration-test",
                Words = [new VerifiedPracticeWord("forgotten", "incorrect")]
            });
            Assert.True(result.Succeeded);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IGamificationService>();
            var retry = await service.ProcessVerifiedAttemptAsync(new VerifiedPracticeAttempt
            {
                UserId = seeded.UserId,
                LessonId = seeded.LessonId,
                SentenceId = seeded.SentenceId,
                PracticeTab = PracticeTabs.Shadowing,
                ExerciseType = ExerciseTypes.Pronunciation,
                TargetScore = 70,
                Score = 40,
                Passed = false,
                IdempotencyKey = "autosave-4",
                AssessmentProvider = "integration-test",
                Words = [new VerifiedPracticeWord("forgotten", "incorrect")]
            });
            Assert.True(retry.AlreadyProcessed);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stat = await db.WordErrorStatistics.SingleAsync(item => item.UserId == seeded.UserId && item.NormalizedWord == "forgotten");
            var vocabulary = await db.VocabularyItems.Where(item => item.UserId == seeded.UserId && item.NormalizedWord == "forgotten").ToListAsync();

            Assert.Equal(4, stat.ConsecutiveErrorCount);
            Assert.Equal(4, stat.TotalErrorCount);
            Assert.Single(vocabulary);
            Assert.Equal("Forgotten means you did not remember it.", vocabulary[0].Meaning);
            Assert.Equal(VocabularySourceTypes.LessonSentence, vocabulary[0].SourceType);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IGamificationService>();
            var reset = await service.ProcessVerifiedAttemptAsync(new VerifiedPracticeAttempt
            {
                UserId = seeded.UserId,
                LessonId = seeded.LessonId,
                SentenceId = seeded.SentenceId,
                PracticeTab = PracticeTabs.Shadowing,
                ExerciseType = ExerciseTypes.Pronunciation,
                TargetScore = 70,
                Score = 95,
                Passed = true,
                IdempotencyKey = "autosave-reset",
                AssessmentProvider = "integration-test",
                Words = [new VerifiedPracticeWord("forgotten", "correct")]
            });
            Assert.True(reset.Succeeded);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stat = await db.WordErrorStatistics.SingleAsync(item => item.UserId == seeded.UserId && item.NormalizedWord == "forgotten");
            var vocabulary = await db.VocabularyItems.SingleAsync(item => item.UserId == seeded.UserId && item.NormalizedWord == "forgotten");

            Assert.Equal(0, stat.ConsecutiveErrorCount);
            Assert.Equal(4, stat.TotalErrorCount);
            Assert.Equal("forgotten", vocabulary.DisplayWord.ToLowerInvariant());
        }
    }

    [ConfiguredSqlServerFact]
    public async Task RewardPenaltyVipRetryExchangeAndConcurrency_AreConsistent()
    {
        using var factory = new AuthFlowApplicationFactory();
        var seeded = await SeedAsync(factory.Services);

        var firstAttempt = AttemptInNewScopeAsync(
            factory.Services,
            NewAttempt(
                seeded.UserId,
                seeded.LessonId,
                seeded.SentenceId,
                "reward-0001",
                passed: true));
        var concurrentRetry = AttemptInNewScopeAsync(
            factory.Services,
            NewAttempt(
                seeded.UserId,
                seeded.LessonId,
                seeded.SentenceId,
                "reward-0001",
                passed: true));
        var attemptResults = await Task.WhenAll(firstAttempt, concurrentRetry);
        var reward = Assert.Single(attemptResults, result => result.Applied);
        var retry = Assert.Single(attemptResults, result => result.AlreadyProcessed);
        Assert.Equal(20, reward.Delta.Exp);
        Assert.Equal(20, reward.Balance.Exp);
        Assert.Equal(0, retry.Delta.Exp);
        Assert.Equal(20, retry.Balance.Exp);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IGamificationService>();
            var secondCompletion = await service.ProcessVerifiedAttemptAsync(NewAttempt(
                seeded.UserId,
                seeded.LessonId,
                seeded.SentenceId,
                "reward-0002",
                passed: true));
            Assert.Equal(0, secondCompletion.Delta.Exp);
            Assert.Equal(20, secondCompletion.Balance.Exp);

            var penalty = await service.ProcessVerifiedAttemptAsync(NewAttempt(
                seeded.UserId,
                seeded.LessonId,
                seeded.SentenceId,
                "penalty-0001",
                passed: false));
            Assert.Equal(-1, penalty.Delta.Hearts);
            Assert.Equal(1, penalty.Balance.Hearts);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(item => item.UserId == seeded.UserId);
            user.IsVip = true;
            await db.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IGamificationService>();
            var vipPenalty = await service.ProcessVerifiedAttemptAsync(NewAttempt(
                seeded.UserId,
                seeded.LessonId,
                seeded.SentenceId,
                "penalty-vip-0001",
                passed: false));
            Assert.Equal(0, vipPenalty.Delta.Hearts);
            Assert.True(vipPenalty.Balance.HasInfiniteHearts);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(item => item.UserId == seeded.UserId);
            var stats = await db.UserStatistics.SingleAsync(item => item.UserId == seeded.UserId);
            user.IsVip = false;
            stats.Exp = 100;
            stats.Hearts = 0;
            await db.SaveChangesAsync();
        }

        var firstExchange = ExchangeInNewScopeAsync(factory.Services, seeded.UserId, "exchange-concurrent-0001");
        var secondExchange = ExchangeInNewScopeAsync(factory.Services, seeded.UserId, "exchange-concurrent-0002");
        var exchangeResults = await Task.WhenAll(firstExchange, secondExchange);
        Assert.Single(exchangeResults, result => result.Succeeded);
        Assert.Single(exchangeResults, result => result.RejectionCode == "insufficient_exp");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stats = await db.UserStatistics.AsNoTracking()
                .SingleAsync(item => item.UserId == seeded.UserId);
            Assert.Equal(0, stats.Exp);
            Assert.Equal(1, stats.Hearts);
            Assert.Equal(1, await db.GamificationLedger.CountAsync(entry =>
                entry.UserId == seeded.UserId
                && entry.SourceType == GamificationSourceTypes.SentenceCompletion));
        }
    }

    private static async Task<GamificationTransactionDto> ExchangeInNewScopeAsync(
        IServiceProvider services,
        long userId,
        string key)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IGamificationService>()
            .ExchangeHeartAsync(userId, key);
    }

    private static async Task<GamificationTransactionDto> AttemptInNewScopeAsync(
        IServiceProvider services,
        VerifiedPracticeAttempt attempt)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IGamificationService>()
            .ProcessVerifiedAttemptAsync(attempt);
    }

    private static VerifiedPracticeAttempt NewAttempt(
        long userId,
        long lessonId,
        long sentenceId,
        string key,
        bool passed) => new()
        {
            UserId = userId,
            LessonId = lessonId,
            SentenceId = sentenceId,
            PracticeTab = PracticeTabs.Shadowing,
            ExerciseType = ExerciseTypes.Pronunciation,
            TargetScore = 70,
            Score = passed ? 90 : 40,
            Passed = passed,
            IdempotencyKey = key,
            AssessmentProvider = "integration-test"
        };

    private static async Task<(long UserId, long LessonId, long SentenceId)> SeedAsync(
        IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = $"gamification-{Guid.NewGuid():N}",
            Email = $"gamification-{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-used",
            FullName = "Gamification Test",
            CreatedAt = now,
            UpdatedAt = now,
            Statistics = new UserStatistic { Hearts = 2, Exp = 0 }
        };
        var course = new Course
        {
            Title = "Gamification Course",
            Level = CourseLevels.Beginner,
            LearningMode = LearningModes.Casual,
            CreatedAt = now,
            UpdatedAt = now,
            Lessons =
            [
                new Lesson
                {
                    Title = "Gamification Lesson",
                    LessonOrder = 1,
                    Duration = 60,
                    Sentences =
                    [
                        new LessonSentence
                        {
                            SentenceOrder = 1,
                            Text = "A verified sentence."
                        }
                    ]
                }
            ]
        };
        db.AddRange(user, course);
        await db.SaveChangesAsync();
        return (user.UserId, course.Lessons.Single().LessonId, course.Lessons.Single().Sentences.Single().SentenceId);
    }
}

internal sealed class StubLanguageReferenceService : ILanguageReferenceService
{
    public Task<WordMeaningDto> GetMeaningAsync(string word, string? context, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WordMeaningDto
        {
            Word = word,
            Ipa = "/fərˈɡɑːtn/",
            Meaning = $"{char.ToUpperInvariant(word[0])}{word[1..].ToLowerInvariant()} means you did not remember it.",
            Provider = "stub"
        });

    public Task<IReadOnlyList<WordIpaDto>> GetIpaBatchAsync(IReadOnlyList<string> words, string accent, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WordIpaDto>>(words.Select(word => new WordIpaDto
        {
            Word = word,
            Ipa = "/fərˈɡɑːtn/"
        }).ToList());
}

internal sealed class ConfiguredSqlServerFactAttribute : FactAttribute
{
    public ConfiguredSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSHADOWING_TEST_SQLSERVER")))
        {
            Skip = "Set WEBSHADOWING_TEST_SQLSERVER to run SQL Server concurrency tests.";
        }
    }
}
