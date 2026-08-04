using System.Text.RegularExpressions;
// Kịch bản test: nâng schema production có dữ liệu cũ, chạy lặp, constraint và concurrency.
// Phụ trách test/seed: Hải Anh. Minh chịu trách nhiệm sửa schema/migration khi test thất bại.
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;
using Xunit;

namespace WebShadowing.DatabaseIntegrationTests;

public sealed class ProductionSchemaIntegrationTests
{
    private const string TestConnectionVariable = "WEBSHADOWING_TEST_SQLSERVER";

    [Fact]
    public async Task Migration_UpgradesPopulatedLegacySchemaAndIsIdempotent()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var now = DateTime.UtcNow;
        var existingUser = NewUser("legacy-with-stats", now);
        existingUser.Statistics = new UserStatistic { Hearts = 4, Exp = 25 };
        var userMissingStatistics = NewUser("legacy-no-stats", now);
        var course = new Course
        {
            Title = "Preserved Legacy Course",
            Level = CourseLevels.Beginner,
            CreatedAt = now,
            UpdatedAt = now,
            Lessons =
            [
                new Lesson
                {
                    Title = "Preserved Legacy Lesson",
                    LessonOrder = 1,
                    Duration = 45,
                    Sentences = [new LessonSentence { SentenceOrder = 1, Text = "Preserve this sentence." }]
                }
            ]
        };
        db.AddRange(existingUser, userMissingStatistics, course);
        await db.SaveChangesAsync();

        await DowngradeToLegacySchemaAsync(db);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.User_Saved_Lessons (user_id, title, learning_mode, content)
            VALUES ({existingUser.UserId}, N'Legacy saved lesson', 'casual', N'Legacy snapshot text');
            """);

        var script = await File.ReadAllTextAsync(Path.Combine(
            AppContext.BaseDirectory,
            "Database",
            "production_learning_schema_update.sql"));
        var batches = Regex.Split(script, @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToArray();
        for (var run = 0; run < 2; run++)
        {
            foreach (var batch in batches)
            {
                await db.Database.ExecuteSqlRawAsync(batch);
            }
        }

        Assert.Equal(2, await ScalarAsync(db, "SELECT COUNT(*) AS [Value] FROM dbo.User_Statistics"));
        Assert.Equal(2, await ScalarAsync(db, "SELECT COUNT(*) AS [Value] FROM dbo.User_Settings"));
        Assert.Equal(1, await ScalarAsync(db, "SELECT COUNT(*) AS [Value] FROM dbo.User_Saved_Lessons"));
        Assert.Equal(1, await ScalarAsync(db, "SELECT COUNT(*) AS [Value] FROM dbo.Saved_AI_Lesson_Segments"));
        Assert.Equal(1, await ScalarAsync(db, "SELECT COUNT(*) AS [Value] FROM dbo.Lesson_Sentences WHERE [text] = N'Preserve this sentence.'"));
        Assert.Equal(13, await ScalarAsync(db, """
            SELECT COUNT(*) AS [Value]
            FROM sys.tables
            WHERE name IN (
                'User_Lesson_Progress', 'User_Sentence_Progress', 'Practice_Attempts',
                'Word_Error_Statistics', 'Vocabulary_Items', 'Favorite_Sentences',
                'User_Settings', 'Mode_Change_History', 'User_Saved_Lessons',
                'Saved_AI_Lesson_Segments', 'VIP_Subscriptions', 'Payment_Transactions',
                'Gamification_Ledger')
            """));
        Assert.Equal(9, await ScalarAsync(db, """
            SELECT COUNT(*) AS [Value]
            FROM sys.columns
            WHERE (object_id = OBJECT_ID('dbo.Users') AND name = 'row_version')
               OR (object_id = OBJECT_ID('dbo.User_Statistics') AND name = 'row_version')
               OR (object_id = OBJECT_ID('dbo.Lesson_Sentences') AND name IN ('start_ms','end_ms'))
               OR (object_id = OBJECT_ID('dbo.Lesson_Material') AND name IN ('source_provider','source_id','license_note','source_review_status','source_reviewed_at'))
            """));
    }

    [Fact]
    public async Task SqlServer_EnforcesUniquenessChecksAndRowVersionConcurrency()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        long settingsId;
        await using (var db = database.CreateContext())
        {
            var now = DateTime.UtcNow;
            var user = NewUser("constraints", now);
            user.Statistics = new UserStatistic { Hearts = 5 };
            var course = new Course
            {
                Title = "Constraint Course",
                Level = CourseLevels.Beginner,
                CreatedAt = now,
                UpdatedAt = now,
                Lessons =
                [
                    new Lesson
                    {
                        Title = "Constraint Lesson",
                        LessonOrder = 1,
                        Duration = 60,
                        Sentences = [new LessonSentence { SentenceOrder = 1, Text = "A durable sentence.", StartMilliseconds = 0, EndMilliseconds = 1000 }]
                    }
                ]
            };
            var settings = new UserSettings { User = user };
            db.AddRange(user, course, settings);
            await db.SaveChangesAsync();
            settingsId = settings.UserSettingsId;
            var lessonId = course.Lessons.Single().LessonId;
            var sentenceId = course.Lessons.Single().Sentences.Single().SentenceId;

            db.UserLessonProgress.AddRange(
                new UserLessonProgress { UserId = user.UserId, LessonId = lessonId, PracticeTab = PracticeTabs.Shadowing },
                new UserLessonProgress { UserId = user.UserId, LessonId = lessonId, PracticeTab = PracticeTabs.Shadowing });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            var statistic = await db.UserStatistics.SingleAsync(s => s.UserId == user.UserId);
            statistic.Hearts = -1;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            db.PracticeAttempts.AddRange(
                NewAttempt(user.UserId, sentenceId, "attempt-retry"),
                NewAttempt(user.UserId, sentenceId, "attempt-retry"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            db.GamificationLedger.AddRange(
                NewLedger(user.UserId, "completion-retry"),
                NewLedger(user.UserId, "completion-retry"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            db.PaymentTransactions.AddRange(
                NewPayment(user.UserId, "payment-retry", "provider-tx-1"),
                NewPayment(user.UserId, "payment-retry", "provider-tx-2"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await using var firstDb = database.CreateContext();
        await using var secondDb = database.CreateContext();
        var firstSettings = await firstDb.UserSettings.SingleAsync(s => s.UserSettingsId == settingsId);
        var secondSettings = await secondDb.UserSettings.SingleAsync(s => s.UserSettingsId == settingsId);
        firstSettings.AutoSaveAiLessons = true;
        secondSettings.Theme = ThemePreferences.Dark;
        await firstDb.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondDb.SaveChangesAsync());
    }

    private static User NewUser(string prefix, DateTime now) => new()
    {
        Username = $"{prefix}-{Guid.NewGuid():N}",
        Email = $"{prefix}-{Guid.NewGuid():N}@example.test",
        PasswordHash = "not-used-by-this-test",
        FullName = "Database Integration Test",
        CreatedAt = now,
        UpdatedAt = now
    };

    private static PracticeAttempt NewAttempt(long userId, long sentenceId, string key) => new()
    {
        UserId = userId,
        SentenceId = sentenceId,
        PracticeTab = PracticeTabs.Shadowing,
        ExerciseType = ExerciseTypes.Pronunciation,
        TargetScore = 70,
        Score = 80,
        Result = AttemptResults.Passed,
        IdempotencyKey = key
    };

    private static PaymentTransaction NewPayment(long userId, string key, string transactionId) => new()
    {
        UserId = userId,
        Provider = "test",
        ProviderTransactionId = transactionId,
        IdempotencyKey = key,
        TransactionType = PaymentTypes.Purchase,
        Status = PaymentStatuses.Succeeded,
        Amount = 100_000,
        Currency = "VND"
    };

    private static GamificationLedgerEntry NewLedger(long userId, string sourceId) => new()
    {
        UserId = userId,
        SourceType = GamificationSourceTypes.SentenceCompletion,
        SourceId = sourceId,
        Reason = "test",
        ExpDelta = 20,
        ExpBalance = 20,
        HeartsBalance = 5,
        StreakBalance = 1,
        CreatedAt = DateTime.UtcNow
    };

    private static Task<int> ScalarAsync(AppDbContext db, string sql) =>
        db.Database.SqlQueryRaw<int>(sql).SingleAsync();

    private static Task DowngradeToLegacySchemaAsync(AppDbContext db) => db.Database.ExecuteSqlRawAsync("""
        DECLARE @dropLegacyFks NVARCHAR(MAX) = N'';
        SELECT @dropLegacyFks = @dropLegacyFks +
            N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' +
            QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
        FROM sys.foreign_keys
        WHERE referenced_object_id IN (
            OBJECT_ID(N'dbo.User_Saved_Lessons'),
            OBJECT_ID(N'dbo.Saved_AI_Lesson_Segments')
        );
        IF LEN(@dropLegacyFks) > 0 EXEC sp_executesql @dropLegacyFks;

        DROP TABLE IF EXISTS dbo.Payment_Transactions;
        DROP TABLE IF EXISTS dbo.Gamification_Ledger;
        DROP TABLE IF EXISTS dbo.VIP_Subscriptions;
        DROP TABLE IF EXISTS dbo.Practice_Attempts;
        DROP TABLE IF EXISTS dbo.Saved_AI_Lesson_Segments;
        DROP TABLE IF EXISTS dbo.User_Saved_Lessons;
        DROP TABLE IF EXISTS dbo.Mode_Change_History;
        DROP TABLE IF EXISTS dbo.Favorite_Sentences;
        DROP TABLE IF EXISTS dbo.Vocabulary_Items;
        DROP TABLE IF EXISTS dbo.Word_Error_Statistics;
        DROP TABLE IF EXISTS dbo.User_Sentence_Progress;
        DROP TABLE IF EXISTS dbo.User_Lesson_Progress;
        DROP TABLE IF EXISTS dbo.User_Settings;

        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Lesson_Sentences') AND name = 'CK_LessonSentences_Timestamps')
            ALTER TABLE dbo.Lesson_Sentences DROP CONSTRAINT CK_LessonSentences_Timestamps;
        IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.Lesson_Sentences') AND name = 'AK_LessonSentences_Lesson_Sentence')
            ALTER TABLE dbo.Lesson_Sentences DROP CONSTRAINT AK_LessonSentences_Lesson_Sentence;
        ALTER TABLE dbo.Lesson_Sentences DROP COLUMN start_ms, end_ms;

        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Lesson_Material') AND name = 'CK_Material_SourceReviewStatus')
            ALTER TABLE dbo.Lesson_Material DROP CONSTRAINT CK_Material_SourceReviewStatus;
        ALTER TABLE dbo.Lesson_Material DROP COLUMN source_provider, source_id, license_note, source_review_status, source_reviewed_at;
        ALTER TABLE dbo.User_Statistics DROP COLUMN row_version;
        ALTER TABLE dbo.Users DROP COLUMN row_version;

        CREATE TABLE dbo.User_Saved_Lessons (
            saved_lesson_id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Legacy_UserSavedLessons PRIMARY KEY,
            user_id BIGINT NOT NULL,
            title NVARCHAR(255) NOT NULL,
            learning_mode VARCHAR(20) NOT NULL,
            content NVARCHAR(MAX) NOT NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_Legacy_UserSavedLessons_CreatedAt DEFAULT (GETDATE()),
            updated_at DATETIME2 NOT NULL CONSTRAINT DF_Legacy_UserSavedLessons_UpdatedAt DEFAULT (GETDATE()),
            CONSTRAINT FK_Legacy_UserSavedLessons_User FOREIGN KEY (user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE,
            CONSTRAINT CK_UserSavedLessons_LearningMode CHECK (learning_mode IN ('casual','academic','professional'))
        );
        """);

    private sealed class SqlServerTestDatabase : IAsyncDisposable
    {
        private readonly string _connectionString;

        private SqlServerTestDatabase(string connectionString) => _connectionString = connectionString;

        public static async Task<SqlServerTestDatabase> CreateAsync()
        {
            var configured = Environment.GetEnvironmentVariable(TestConnectionVariable)
                ?? "Server=localhost;Database=ignored;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True";
            var builder = new SqlConnectionStringBuilder(configured)
            {
                InitialCatalog = $"EnglishShadowingDB_Test_{Guid.NewGuid():N}"
            };
            var database = new SqlServerTestDatabase(builder.ConnectionString);
            await using var db = database.CreateContext();
            await db.Database.EnsureCreatedAsync();
            return database;
        }

        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;
            return new AppDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = CreateContext();
            await db.Database.EnsureDeletedAsync();
        }
    }
}
