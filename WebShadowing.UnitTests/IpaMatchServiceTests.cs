using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class IpaMatchServiceTests
{
    [Fact]
    public async Task GetQuestionAsync_ReturnsSignedQuestion_WithOptions()
    {
        await using var db = CreateDbContext();

        var service = new IpaMatchService(
            db,
            new FakeCourseService(),
            new FakeUserContextService(),
            new FakeLanguageReferenceService(),
            new FakeGamificationService(),
            new MemoryCache(new MemoryCacheOptions()),
            DataProtectionProvider.Create("IpaMatchServiceTests"));

        var question = await service.GetQuestionAsync(new GetIpaMatchQuestionCommand(11, 101));

        Assert.False(string.IsNullOrWhiteSpace(question.QuestionToken));
        Assert.Equal("hello", question.PromptWord, ignoreCase: true);
        Assert.NotEmpty(question.Options);
        Assert.Contains(question.Options, option => option.Ipa == "/həˈloʊ/");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class FakeLanguageReferenceService : ILanguageReferenceService
    {
        public Task<WordMeaningDto> GetMeaningAsync(string word, string? context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new WordMeaningDto { Word = word, Meaning = "meaning", Ipa = "/həˈloʊ/" });
        }

        public Task<IReadOnlyList<WordIpaDto>> GetIpaBatchAsync(IReadOnlyList<string> words, string accent, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WordIpaDto> result = words.Select(word => new WordIpaDto
            {
                Word = word,
                Ipa = word.Equals("hello", StringComparison.OrdinalIgnoreCase) ? "/həˈloʊ/" : "/wɝːld/"
            }).ToList();
            return Task.FromResult(result);
        }
    }

    private sealed class FakeGamificationService : IGamificationService
    {
        public Task<GamificationBalanceDto?> GetBalanceAsync(long userId, CancellationToken cancellationToken = default) => Task.FromResult<GamificationBalanceDto?>(new GamificationBalanceDto());
        public Task<GamificationTransactionDto> ProcessVerifiedAttemptAsync(VerifiedPracticeAttempt attempt, CancellationToken cancellationToken = default) => Task.FromResult(new GamificationTransactionDto { Succeeded = true, Balance = new GamificationBalanceDto() });
        public Task<GamificationTransactionDto> ExchangeHeartAsync(long userId, string idempotencyKey, CancellationToken cancellationToken = default) => Task.FromResult(new GamificationTransactionDto { Succeeded = true, Balance = new GamificationBalanceDto() });
    }

    private sealed class FakeCourseService : ICourseService
    {
        public Task<LessonLookupResult> GetLessonAsync(long lessonId, string learningMode, byte pronunciationTarget, CancellationToken cancellationToken = default)
        {
            var lesson = new LessonDetailDto
            {
                LessonId = lessonId,
                Title = "Mock Lesson",
                Sentences =
                [
                    new LessonSentenceDto
                    {
                        SentenceId = 101,
                        Text = "Hello world",
                        Order = 1,
                        Ipa = "/həˈloʊ wɝːld/"
                    }
                ]
            };

            return Task.FromResult(LessonLookupResult.Found(lesson));
        }

        public Task<LibraryResponseDto> GetLibraryAsync(string learningMode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CoursesListResponseDto> GetCoursesAsync(string courseType, string learningMode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<CourseDetailDto?> GetCourseAsync(long courseId, string learningMode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeUserContextService : IUserContextService
    {
        public bool IsAuthenticated => true;

        public Task<long?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<long?>(1);
        public long? GetCurrentUserId() => 1;
        public Task<string> GetLearningModeAsync(CancellationToken cancellationToken = default) => Task.FromResult(LearningModes.Casual);
        public Task<string> GetAccentAsync(CancellationToken cancellationToken = default) => Task.FromResult(Accents.EnUs);
        public Task<byte> GetPronunciationTargetAsync(CancellationToken cancellationToken = default) => Task.FromResult((byte)70);
    }
}