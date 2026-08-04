using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class PracticeEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAsync_UsesIdempotencyKey_AndAvoidsDuplicateAttempts()
    {
        await using var db = CreateDbContext();
        var provider = new FakeAssessmentService(
            new PronunciationAssessmentResult
            {
                Provider = "azure-speech",
                OverallScore = 75,
                Transcript = "hello world",
                Feedback = "good",
                Words =
                [
                    new PronunciationWordResult { Word = "hello", AccuracyCode = "correct" },
                    new PronunciationWordResult { Word = "world", AccuracyCode = "warning" }
                ]
            });
        var service = CreateService(db, provider);
        var command = new EvaluateShadowingCommand(
            LessonId: 11,
            SentenceId: 101,
            SentenceIndex: 0,
            Audio: BuildWavBytes(durationSeconds: 2),
            AudioFormat: "wav",
            ContentType: "audio/wav",
            IdempotencyKey: "same-key");

        var first = await service.EvaluateAsync(command);
        var second = await service.EvaluateAsync(command);

        Assert.True(first.Passed);
        Assert.True(second.Passed);
        Assert.Equal(1, provider.CallCount);
        Assert.Equal(1, await db.PracticeAttempts.CountAsync());
    }

    [Fact]
    public async Task EvaluateAsync_UpdatesWordErrorStreaks_AcrossAttempts()
    {
        await using var db = CreateDbContext();
        var provider = new QueueAssessmentService(new Queue<PronunciationAssessmentResult>(
        [
            new PronunciationAssessmentResult
            {
                Provider = "azure-speech",
                OverallScore = 60,
                Transcript = "helo",
                Feedback = "retry",
                Words = [new PronunciationWordResult { Word = "hello", AccuracyCode = "incorrect" }]
            },
            new PronunciationAssessmentResult
            {
                Provider = "azure-speech",
                OverallScore = 62,
                Transcript = "helo",
                Feedback = "retry",
                Words = [new PronunciationWordResult { Word = "hello", AccuracyCode = "warning" }]
            },
            new PronunciationAssessmentResult
            {
                Provider = "azure-speech",
                OverallScore = 90,
                Transcript = "hello",
                Feedback = "great",
                Words = [new PronunciationWordResult { Word = "hello", AccuracyCode = "correct" }]
            }
        ]));
        var service = CreateService(db, provider);

        await service.EvaluateAsync(NewCommand("k1"));
        await service.EvaluateAsync(NewCommand("k2"));
        await service.EvaluateAsync(NewCommand("k3"));

        var stat = await db.WordErrorStatistics.SingleAsync(item => item.NormalizedWord == "hello");
        Assert.Equal(0, stat.ConsecutiveErrorCount);
        Assert.Equal(2, stat.TotalErrorCount);

        EvaluateShadowingCommand NewCommand(string key) => new(
            LessonId: 11,
            SentenceId: 101,
            SentenceIndex: 0,
            Audio: BuildWavBytes(durationSeconds: 2),
            AudioFormat: "wav",
            ContentType: "audio/wav",
            IdempotencyKey: key);
    }

    [Fact]
    public async Task EvaluateAsync_RejectsInvalidAudioMime()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, new FakeAssessmentService(new PronunciationAssessmentResult()));

        var exception = await Assert.ThrowsAsync<PronunciationAssessmentUnavailableException>(() =>
            service.EvaluateAsync(new EvaluateShadowingCommand(
                LessonId: 11,
                SentenceId: 101,
                SentenceIndex: 0,
                Audio: BuildWavBytes(durationSeconds: 1),
                AudioFormat: "wav",
                ContentType: "text/plain",
                IdempotencyKey: "mime-key")));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("invalid_audio_mime", exception.ErrorCode);
    }

    [Fact]
    public async Task EvaluateAsync_RejectsAudioBeyondDurationLimit()
    {
        await using var db = CreateDbContext();
        var options = new PronunciationAssessmentOptions
        {
            MaxAudioDurationSeconds = 3
        };
        var service = CreateService(
            db,
            new FakeAssessmentService(new PronunciationAssessmentResult()),
            Options.Create(options));

        var exception = await Assert.ThrowsAsync<PronunciationAssessmentUnavailableException>(() =>
            service.EvaluateAsync(new EvaluateShadowingCommand(
                LessonId: 11,
                SentenceId: 101,
                SentenceIndex: 0,
                Audio: BuildWavBytes(durationSeconds: 4),
                AudioFormat: "wav",
                ContentType: "audio/wav",
                IdempotencyKey: "too-long")));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("audio_duration_exceeded", exception.ErrorCode);
    }

    [Fact]
    public async Task EvaluateAsync_AllowsAudioAtDurationLimit()
    {
        await using var db = CreateDbContext();
        var options = new PronunciationAssessmentOptions
        {
            MaxAudioDurationSeconds = 3
        };
        var service = CreateService(
            db,
            new FakeAssessmentService(new PronunciationAssessmentResult
            {
                Provider = "azure-speech",
                OverallScore = 80,
                Transcript = "hello",
                Feedback = "ok"
            }),
            Options.Create(options));

        var result = await service.EvaluateAsync(new EvaluateShadowingCommand(
            LessonId: 11,
            SentenceId: 101,
            SentenceIndex: 0,
            Audio: BuildWavBytes(durationSeconds: 3),
            AudioFormat: "wav",
            ContentType: "audio/wav",
            IdempotencyKey: "at-limit"));

        Assert.Equal(80, result.Score);
        Assert.Single(await db.PracticeAttempts.ToListAsync());
    }

    private static PracticeEvaluationService CreateService(
        AppDbContext db,
        IPronunciationAssessmentService assessmentService,
        IOptions<PronunciationAssessmentOptions>? options = null,
        IGamificationService? gamificationService = null)
    {
        var effectiveOptions = options ?? Options.Create(new PronunciationAssessmentOptions());
        var profileService = new PronunciationScoreProfileService(effectiveOptions);
        return new PracticeEvaluationService(
            db,
            new FakeCourseService(),
            new FakeUserContextService(),
            assessmentService,
            gamificationService ?? new FakeGamificationService(db),
            profileService,
            effectiveOptions);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new AppDbContext(options);
        SeedPracticeData(db);
        return db;
    }

    private static void SeedPracticeData(AppDbContext db)
    {
        db.Users.Add(new User
        {
            UserId = 7,
            Username = "tester",
            Email = "tester@example.com",
            PasswordHash = "hash",
            FullName = "Test User",
            LearningMode = LearningModes.Casual,
            Accent = Accents.EnUs,
            PronunciationTarget = PronunciationTargets.Comprehension70
        });

        db.Courses.Add(new Course
        {
            CourseId = 1,
            Title = "Mock Course",
            Level = "Beginner",
            LearningMode = LearningModes.Casual,
            CourseType = CourseTypes.Curriculum
        });

        db.Lessons.Add(new Lesson
        {
            LessonId = 11,
            CourseId = 1,
            Title = "Mock Lesson",
            LessonOrder = 1,
            Duration = 60
        });

        db.LessonSentences.Add(new LessonSentence
        {
            SentenceId = 101,
            LessonId = 11,
            SentenceOrder = 1,
            Text = "hello",
            Ipa = "/həˈloʊ/"
        });

        db.SaveChanges();
    }

    private static byte[] BuildWavBytes(int durationSeconds)
    {
        const int sampleRate = 16000;
        const short channels = 1;
        const short bitsPerSample = 16;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var dataSize = byteRate * durationSeconds;
        var totalSize = 44 + dataSize;
        var bytes = new byte[totalSize];

        WriteAscii(bytes, 0, "RIFF");
        BitConverter.GetBytes(totalSize - 8).CopyTo(bytes, 4);
        WriteAscii(bytes, 8, "WAVE");
        WriteAscii(bytes, 12, "fmt ");
        BitConverter.GetBytes(16).CopyTo(bytes, 16);
        BitConverter.GetBytes((short)1).CopyTo(bytes, 20);
        BitConverter.GetBytes(channels).CopyTo(bytes, 22);
        BitConverter.GetBytes(sampleRate).CopyTo(bytes, 24);
        BitConverter.GetBytes(byteRate).CopyTo(bytes, 28);
        BitConverter.GetBytes((short)(channels * bitsPerSample / 8)).CopyTo(bytes, 32);
        BitConverter.GetBytes(bitsPerSample).CopyTo(bytes, 34);
        WriteAscii(bytes, 36, "data");
        BitConverter.GetBytes(dataSize).CopyTo(bytes, 40);

        return bytes;
    }

    private static void WriteAscii(byte[] destination, int offset, string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            destination[offset + i] = (byte)value[i];
        }
    }

    private sealed class FakeCourseService : ICourseService
    {
        public Task<LibraryResponseDto> GetLibraryAsync(string learningMode, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CoursesListResponseDto> GetCoursesAsync(string courseType, string learningMode, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<CourseDetailDto?> GetCourseAsync(long courseId, string learningMode, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

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
                        Text = "hello",
                        Ipa = "/həˈloʊ/",
                        Order = 1
                    }
                ]
            };
            return Task.FromResult(LessonLookupResult.Found(lesson));
        }
    }

    private sealed class FakeUserContextService : IUserContextService
    {
        public bool IsAuthenticated => true;
        public long? GetCurrentUserId() => 7;
        public Task<string> GetLearningModeAsync(CancellationToken cancellationToken = default) => Task.FromResult(LearningModes.Casual);
        public Task<byte> GetPronunciationTargetAsync(CancellationToken cancellationToken = default) => Task.FromResult(PronunciationTargets.Comprehension70);
        public Task<string> GetAccentAsync(CancellationToken cancellationToken = default) => Task.FromResult(Accents.EnUs);
    }

    private sealed class FakeGamificationService : IGamificationService
    {
        private readonly AppDbContext _db;

        public FakeGamificationService(AppDbContext db)
        {
            _db = db;
        }

        public Task<GamificationTransactionDto> ProcessVerifiedAttemptAsync(VerifiedPracticeAttempt attempt, CancellationToken cancellationToken = default)
        {
            var existingAttempt = _db.PracticeAttempts.SingleOrDefault(item =>
                item.UserId == attempt.UserId && item.IdempotencyKey == attempt.IdempotencyKey);
            if (existingAttempt is not null)
            {
                return Task.FromResult(new GamificationTransactionDto
                {
                    Succeeded = true,
                    Applied = false,
                    AlreadyProcessed = true,
                    TransactionType = "attempt",
                    Balance = new GamificationBalanceDto()
                });
            }

            var practiceAttempt = new PracticeAttempt
            {
                UserId = attempt.UserId,
                SentenceId = attempt.SentenceId,
                PracticeTab = attempt.PracticeTab,
                ExerciseType = attempt.ExerciseType,
                TargetScore = attempt.TargetScore,
                Score = attempt.Score,
                Result = attempt.Passed ? AttemptResults.Passed : AttemptResults.Failed,
                AssessmentProvider = attempt.AssessmentProvider,
                ProviderReferenceId = attempt.ProviderReferenceId,
                TranscriptText = attempt.TranscriptText,
                FeedbackText = attempt.FeedbackText,
                IdempotencyKey = attempt.IdempotencyKey,
                AttemptedAt = DateTime.UtcNow
            };
            _db.PracticeAttempts.Add(practiceAttempt);

            foreach (var word in attempt.Words)
            {
                var normalizedWord = NormalizeWord(word.Word);
                if (normalizedWord.Length == 0)
                {
                    continue;
                }

                var statistic = _db.WordErrorStatistics.SingleOrDefault(item =>
                    item.UserId == attempt.UserId && item.NormalizedWord == normalizedWord);
                if (statistic is null)
                {
                    statistic = new WordErrorStatistic
                    {
                        UserId = attempt.UserId,
                        NormalizedWord = normalizedWord,
                        DisplayWord = word.Word
                    };
                    _db.WordErrorStatistics.Add(statistic);
                }

                statistic.LastAttemptedAt = DateTime.UtcNow;
                if (string.Equals(word.AccuracyCode, "correct", StringComparison.OrdinalIgnoreCase))
                {
                    statistic.ConsecutiveErrorCount = 0;
                }
                else
                {
                    statistic.ConsecutiveErrorCount++;
                    statistic.TotalErrorCount++;
                    statistic.LastErrorAt = DateTime.UtcNow;
                    statistic.LastSentenceId = attempt.SentenceId;
                }
            }

            _db.SaveChanges();

            return Task.FromResult(new GamificationTransactionDto
            {
                Succeeded = true,
                Applied = true,
                TransactionType = "attempt",
                Balance = new GamificationBalanceDto()
            });
        }

        public Task<GamificationTransactionDto> ExchangeHeartAsync(long userId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GamificationTransactionDto
            {
                Succeeded = true,
                Applied = true,
                TransactionType = "heart_exchange",
                Balance = new GamificationBalanceDto()
            });

        public Task<GamificationBalanceDto?> GetBalanceAsync(long userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GamificationBalanceDto?>(new GamificationBalanceDto());

        private static string NormalizeWord(string value)
        {
            return new string(value
                .ToLowerInvariant()
                .Where(character => char.IsLetter(character) || character == '-' || character == '\'')
                .ToArray());
        }
    }

    private sealed class FakeAssessmentService : IPronunciationAssessmentService
    {
        private readonly PronunciationAssessmentResult _result;
        public int CallCount { get; private set; }

        public FakeAssessmentService(PronunciationAssessmentResult result)
        {
            _result = result;
        }

        public Task<PronunciationAssessmentResult> AssessAsync(PronunciationAssessmentRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

    private sealed class QueueAssessmentService : IPronunciationAssessmentService
    {
        private readonly Queue<PronunciationAssessmentResult> _results;

        public QueueAssessmentService(Queue<PronunciationAssessmentResult> results)
        {
            _results = results;
        }

        public Task<PronunciationAssessmentResult> AssessAsync(PronunciationAssessmentRequest request, CancellationToken cancellationToken = default)
        {
            if (_results.Count == 0)
            {
                throw new InvalidOperationException("No more mock results.");
            }

            return Task.FromResult(_results.Dequeue());
        }
    }
}
