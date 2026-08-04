using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.AuthFlowTests;

public sealed class AuthFlowEndToEndTests
{
    [Fact]
    public async Task ProductionSchemaScript_UpgradesPopulatedLegacyDatabaseAndCanRunTwice()
    {
        using var factory = new AuthFlowApplicationFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var existingUser = new User
        {
            Username = $"legacy-with-stats-{Guid.NewGuid():N}",
            Email = $"legacy-with-stats-{Guid.NewGuid():N}@example.test",
            PasswordHash = "legacy-hash",
            FullName = "Legacy User With Stats",
            CreatedAt = now,
            UpdatedAt = now,
            Statistics = new UserStatistic { Hearts = 4, Exp = 25 }
        };
        var userMissingStatistics = new User
        {
            Username = $"legacy-no-stats-{Guid.NewGuid():N}",
            Email = $"legacy-no-stats-{Guid.NewGuid():N}@example.test",
            PasswordHash = "legacy-hash",
            FullName = "Legacy User Without Stats",
            CreatedAt = now,
            UpdatedAt = now
        };
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
        var existingUserId = existingUser.UserId;

        // This database is isolated and disposable. Remove only the extension
        // objects/columns to reproduce the last production schema before this update.
        await db.Database.ExecuteSqlRawAsync("""
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
            DROP TABLE IF EXISTS dbo.VIP_Subscriptions;
            DROP TABLE IF EXISTS dbo.Gamification_Ledger;
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
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.User_Saved_Lessons (user_id, title, learning_mode, content)
            VALUES ({existingUserId}, N'Legacy saved lesson', 'casual', N'Legacy snapshot text');
            """);

        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var scriptPath = Path.Combine(
            environment.ContentRootPath,
            "Database",
            "production_learning_schema_update.sql");
        var script = await File.ReadAllTextAsync(scriptPath);
        var batches = Regex.Split(
                script,
                @"^\s*GO\s*(?:--.*)?$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(batch => !string.IsNullOrWhiteSpace(batch));

        for (var run = 0; run < 2; run++)
        {
            foreach (var batch in batches)
            {
                await db.Database.ExecuteSqlRawAsync(batch);
            }
        }

        var tableCount = await db.Database
            .SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value]
                FROM sys.tables
                WHERE name IN (
                    'User_Lesson_Progress', 'User_Sentence_Progress', 'Practice_Attempts',
                    'Word_Error_Statistics', 'Vocabulary_Items', 'Favorite_Sentences',
                    'User_Settings', 'Mode_Change_History', 'User_Saved_Lessons',
                    'Saved_AI_Lesson_Segments', 'VIP_Subscriptions', 'Payment_Transactions')
                """)
            .SingleAsync();

        Assert.Equal(12, tableCount);

        var statisticsCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM dbo.User_Statistics").SingleAsync();
        var settingsCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM dbo.User_Settings").SingleAsync();
        var savedLessonCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM dbo.User_Saved_Lessons").SingleAsync();
        var savedSegmentCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM dbo.Saved_AI_Lesson_Segments").SingleAsync();
        var preservedSentenceCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM dbo.Lesson_Sentences WHERE [text] = N'Preserve this sentence.'").SingleAsync();
        var restoredColumnCount = await db.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS [Value]
            FROM sys.columns
            WHERE (object_id = OBJECT_ID('dbo.Users') AND name = 'row_version')
               OR (object_id = OBJECT_ID('dbo.User_Statistics') AND name = 'row_version')
               OR (object_id = OBJECT_ID('dbo.Lesson_Sentences') AND name IN ('start_ms','end_ms'))
               OR (object_id = OBJECT_ID('dbo.Lesson_Material') AND name IN ('source_provider','source_id','license_note','source_review_status','source_reviewed_at'))
            """).SingleAsync();

        Assert.Equal(2, statisticsCount);
        Assert.Equal(2, settingsCount);
        Assert.Equal(1, savedLessonCount);
        Assert.Equal(1, savedSegmentCount);
        Assert.Equal(1, preservedSentenceCount);
        Assert.Equal(9, restoredColumnCount);
    }

    [Fact]
    public async Task ProductionSchema_EnforcesUniquenessChecksAndRowVersionConcurrency()
    {
        using var factory = new AuthFlowApplicationFactory();
        using var client = factory.CreateClient();
        long userId;
        long lessonId;
        long sentenceId;
        long settingsId;

        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            var user = new User
            {
                Username = $"schema-{Guid.NewGuid():N}",
                Email = $"schema-{Guid.NewGuid():N}@example.test",
                PasswordHash = "not-used-by-this-test",
                FullName = "Schema Test",
                CreatedAt = now,
                UpdatedAt = now,
                Statistics = new UserStatistic { Hearts = 5 }
            };
            var course = new Course
            {
                Title = "Schema Test Course",
                Level = CourseLevels.Beginner,
                CreatedAt = now,
                UpdatedAt = now,
                Lessons =
                [
                    new Lesson
                    {
                        Title = "Schema Test Lesson",
                        LessonOrder = 1,
                        Duration = 60,
                        Sentences =
                        [
                            new LessonSentence
                            {
                                SentenceOrder = 1,
                                Text = "A durable sentence.",
                                StartMilliseconds = 0,
                                EndMilliseconds = 1000
                            }
                        ]
                    }
                ]
            };
            var settings = new UserSettings { User = user };
            db.AddRange(user, course, settings);
            await db.SaveChangesAsync();
            userId = user.UserId;
            lessonId = course.Lessons.Single().LessonId;
            sentenceId = course.Lessons.Single().Sentences.Single().SentenceId;
            settingsId = settings.UserSettingsId;

            db.UserLessonProgress.AddRange(
                new UserLessonProgress { UserId = userId, LessonId = lessonId, PracticeTab = PracticeTabs.Shadowing },
                new UserLessonProgress { UserId = userId, LessonId = lessonId, PracticeTab = PracticeTabs.Shadowing });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            var statistic = await db.UserStatistics.SingleAsync(s => s.UserId == userId);
            statistic.Hearts = -1;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            db.PracticeAttempts.AddRange(
                NewAttempt(userId, sentenceId, "attempt-retry"),
                NewAttempt(userId, sentenceId, "attempt-retry"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();

            db.PaymentTransactions.AddRange(
                NewPayment(userId, "payment-retry", "provider-tx-1"),
                NewPayment(userId, "payment-retry", "provider-tx-2"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await using var firstScope = factory.Services.CreateAsyncScope();
        await using var secondScope = factory.Services.CreateAsyncScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstSettings = await firstDb.UserSettings.SingleAsync(s => s.UserSettingsId == settingsId);
        var secondSettings = await secondDb.UserSettings.SingleAsync(s => s.UserSettingsId == settingsId);
        firstSettings.AutoSaveAiLessons = true;
        secondSettings.Theme = ThemePreferences.Dark;
        await firstDb.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondDb.SaveChangesAsync());
    }

    private static PracticeAttempt NewAttempt(long userId, long sentenceId, string idempotencyKey) => new()
    {
        UserId = userId,
        SentenceId = sentenceId,
        PracticeTab = PracticeTabs.Shadowing,
        ExerciseType = ExerciseTypes.Pronunciation,
        TargetScore = 70,
        Score = 80,
        Result = AttemptResults.Passed,
        IdempotencyKey = idempotencyKey
    };

    private static PaymentTransaction NewPayment(long userId, string idempotencyKey, string providerTransactionId) => new()
    {
        UserId = userId,
        Provider = "test",
        ProviderTransactionId = providerTransactionId,
        IdempotencyKey = idempotencyKey,
        TransactionType = PaymentTypes.Purchase,
        Status = PaymentStatuses.Succeeded,
        Amount = 100_000,
        Currency = "VND"
    };

    [Fact]
    public async Task Register_Onboarding_Login_PersistsPreferencesAndEnforcesGuard()
    {
        using var factory = new AuthFlowApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"auth-flow-{Guid.NewGuid():N}@example.test";
        const string password = "Shadow123!";

        var registerPage = await client.GetAsync("/Home/Authen?step=register");
        var registerToken = await ReadAntiForgeryTokenAsync(registerPage);
        var registerResponse = await client.PostAsync("/Account/Register", Form(
            ("__RequestVerificationToken", registerToken),
            ("Register.FullName", "Auth Flow Test"),
            ("Register.Email", email),
            ("Register.Password", password)));

        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);
        Assert.Contains("step=level", registerResponse.Headers.Location?.OriginalString);

        var blockedBeforeOnboarding = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, blockedBeforeOnboarding.StatusCode);
        Assert.Contains("/Home/Authen?step=level", blockedBeforeOnboarding.Headers.Location?.OriginalString);

        var goalPage = await client.GetAsync("/Home/Authen?step=goal&learningMode=academic&accent=en-gb");
        var onboardingToken = await ReadAntiForgeryTokenAsync(goalPage);
        using var onboardingRequest = new HttpRequestMessage(HttpMethod.Put, "/api/user/onboarding")
        {
            Content = JsonContent.Create(new CompleteOnboardingViewModel
            {
                LearningMode = LearningModes.Academic,
                PronunciationTarget = PronunciationTargets.Accent90,
                Accent = Accents.EnGb,
                Plan = "vip"
            })
        };
        onboardingRequest.Headers.Add("RequestVerificationToken", onboardingToken);

        var onboardingResponse = await client.SendAsync(onboardingRequest);
        Assert.Equal(HttpStatusCode.OK, onboardingResponse.StatusCode);
        var onboarding = await onboardingResponse.Content.ReadFromJsonAsync<CompleteOnboardingResponseDto>();
        Assert.NotNull(onboarding);
        Assert.True(onboarding.User.OnboardingCompleted);
        Assert.True(onboarding.User.IsVip);
        Assert.Equal("demo_stub", onboarding.User.VipEntitlementSource);

        var me = await client.GetFromJsonAsync<UserMeDto>("/api/user/me");
        Assert.NotNull(me);
        Assert.Equal(LearningModes.Academic, me.LearningMode);
        Assert.Equal(PronunciationTargets.Accent90, me.PronunciationTarget);
        Assert.Equal(Accents.EnGb, me.Accent);
        Assert.True(me.IsVip);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await db.Users.AsNoTracking().SingleAsync(user => user.Email == email);
            Assert.True(persisted.OnboardingCompleted);
            Assert.Equal(LearningModes.Academic, persisted.LearningMode);
            Assert.Equal(PronunciationTargets.Accent90, persisted.PronunciationTarget);
            Assert.Equal(Accents.EnGb, persisted.Accent);
            Assert.True(persisted.IsVip);
            var stats = await db.UserStatistics.SingleAsync(stat => stat.UserId == persisted.UserId);
            stats.StreakDays = 12;
            stats.Hearts = 4;
            stats.Exp = 980;
            await db.SaveChangesAsync();
        }

        var courseResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, courseResponse.StatusCode);
        var courseHtml = await courseResponse.Content.ReadAsStringAsync();
        Assert.Contains("aria-label=\"Chuỗi học 12 ngày\"", courseHtml);
        Assert.Contains("data-gamification-stat=\"hearts\"", courseHtml);
        Assert.Contains("aria-label=\"980 điểm kinh nghiệm\"", courseHtml);
        Assert.Contains("provip-box--vip", courseHtml);
        Assert.Contains("mobile-gamification-bar", courseHtml);
        Assert.Contains("mobile-gamification-link", courseHtml);
        Assert.Contains("Tiến trình <span>&amp; Thẻ nhớ</span>", courseHtml);

        var settingsResponse = await client.GetAsync("/Home/Settings");
        Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);
        var settingsHtml = await settingsResponse.Content.ReadAsStringAsync();
        Assert.Contains("settings-mobile-tools", settingsHtml);
        Assert.Contains("Giao diện tối", settingsHtml);
        Assert.Contains("Đăng xuất", settingsHtml);

        var authPageAfterOnboarding = await client.GetAsync("/Home/Authen");
        Assert.Equal(HttpStatusCode.Redirect, authPageAfterOnboarding.StatusCode);

        var logoutToken = await ReadAntiForgeryTokenAsync(courseResponse);
        var logoutResponse = await client.PostAsync("/Account/Logout", Form(
            ("__RequestVerificationToken", logoutToken)));
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);

        var loginPage = await client.GetAsync("/Home/Authen");
        var loginToken = await ReadAntiForgeryTokenAsync(loginPage);
        var loginResponse = await client.PostAsync("/Account/Login", Form(
            ("__RequestVerificationToken", loginToken),
            ("Login.Email", email),
            ("Login.Password", password)));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/", loginResponse.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task VipPlan_IsRejectedWhenServerStubIsDisabled()
    {
        using var factory = new AuthFlowApplicationFactory(vipStubEnabled: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"vip-guard-{Guid.NewGuid():N}@example.test";
        var registerPage = await client.GetAsync("/Home/Authen?step=register");
        var registerToken = await ReadAntiForgeryTokenAsync(registerPage);
        var registerResponse = await client.PostAsync("/Account/Register", Form(
            ("__RequestVerificationToken", registerToken),
            ("Register.FullName", "VIP Guard Test"),
            ("Register.Email", email),
            ("Register.Password", "Shadow123!")));
        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);

        var goalPage = await client.GetAsync("/Home/Authen?step=goal");
        var onboardingToken = await ReadAntiForgeryTokenAsync(goalPage);
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/user/onboarding")
        {
            Content = JsonContent.Create(new CompleteOnboardingViewModel
            {
                LearningMode = LearningModes.Casual,
                PronunciationTarget = PronunciationTargets.Comprehension70,
                Accent = Accents.EnUs,
                Plan = "vip"
            })
        };
        request.Headers.Add("RequestVerificationToken", onboardingToken);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var me = await client.GetFromJsonAsync<UserMeDto>("/api/user/me");
        Assert.NotNull(me);
        Assert.False(me.IsVip);
        Assert.False(me.OnboardingCompleted);
        Assert.Equal("none", me.VipEntitlementSource);
    }

    [ConfiguredSqlServerFact]
    public async Task PracticeModes_PersistAttemptAndGamificationOnce()
    {
        using var factory = new AuthFlowApplicationFactory(
            configureServices: services =>
            {
                services.RemoveAll<IPronunciationAssessmentService>();
                services.AddSingleton<IPronunciationAssessmentService>(
                    new SuccessfulPronunciationAssessmentService());
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"practice-flow-{Guid.NewGuid():N}@example.test";
        var registerPage = await client.GetAsync("/Home/Authen?step=register");
        var registerToken = await ReadAntiForgeryTokenAsync(registerPage);
        var registerResponse = await client.PostAsync("/Account/Register", Form(
            ("__RequestVerificationToken", registerToken),
            ("Register.FullName", "Practice Flow Test"),
            ("Register.Email", email),
            ("Register.Password", "Shadow123!")));
        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);

        var statsPage = await client.GetAsync("/Home/Stats");
        Assert.Equal(HttpStatusCode.OK, statsPage.StatusCode);
        var statsHtml = await statsPage.Content.ReadAsStringAsync();
        Assert.Contains("mobile-gamification-bar", statsHtml);
        Assert.Contains("mobile-gamification-link", statsHtml);
        Assert.Contains("data-gamification-stat=\"hearts\"", statsHtml);

        long userId;
        long lessonId;
        long sentenceId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(item => item.Email == email);
            var now = DateTime.UtcNow;
            var course = new Course
            {
                Title = "Production Practice Flow",
                Level = CourseLevels.Beginner,
                LearningMode = LearningModes.Casual,
                CreatedAt = now,
                UpdatedAt = now,
                Lessons =
                [
                    new Lesson
                    {
                        Title = "Verified Shadowing",
                        LessonOrder = 1,
                        Duration = 60,
                        Sentences =
                        [
                            new LessonSentence
                            {
                                SentenceOrder = 1,
                                Text = "A verified production attempt.",
                                Ipa = "/ə ˈverɪfaɪd prəˈdʌkʃən əˈtempt/"
                            }
                        ]
                    }
                ]
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            userId = user.UserId;
            lessonId = course.Lessons.Single().LessonId;
            sentenceId = course.Lessons.Single().Sentences.Single().SentenceId;
        }

        const string idempotencyKey = "practice-flow-attempt-0001";
        var firstResponse = await PostShadowingAsync(
            client,
            lessonId,
            sentenceId,
            idempotencyKey);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<ShadowingEvaluationDto>();
        Assert.NotNull(first);
        Assert.True(first.Passed);
        Assert.NotNull(first.Gamification);
        Assert.True(first.Gamification.Applied);
        Assert.Equal(20, first.Gamification.Delta.Exp);
        Assert.Equal(20, first.Gamification.Balance.Exp);

        var retryResponse = await PostShadowingAsync(
            client,
            lessonId,
            sentenceId,
            idempotencyKey);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retry = await retryResponse.Content.ReadFromJsonAsync<ShadowingEvaluationDto>();
        Assert.NotNull(retry);
        Assert.NotNull(retry.Gamification);
        Assert.True(retry.Gamification.AlreadyProcessed);
        Assert.Equal(20, retry.Gamification.Balance.Exp);

        const string failedDictationKey = "dictation-attempt-0001";
        var failedDictationResponse = await PostPracticeAnswerAsync(
            client,
            lessonId,
            sentenceId,
            PracticeTabs.Dictation,
            "wrong answer",
            failedDictationKey);
        Assert.Equal(HttpStatusCode.OK, failedDictationResponse.StatusCode);
        var failedDictation = await failedDictationResponse.Content
            .ReadFromJsonAsync<PracticeAnswerEvaluationDto>();
        Assert.NotNull(failedDictation);
        Assert.False(failedDictation.Passed);
        Assert.Equal(-1, failedDictation.Gamification.Delta.Hearts);
        Assert.Equal(4, failedDictation.Gamification.Balance.Hearts);

        var dictationRetryResponse = await PostPracticeAnswerAsync(
            client,
            lessonId,
            sentenceId,
            PracticeTabs.Dictation,
            "A verified production attempt.",
            failedDictationKey);
        var dictationRetry = await dictationRetryResponse.Content
            .ReadFromJsonAsync<PracticeAnswerEvaluationDto>();
        Assert.NotNull(dictationRetry);
        Assert.False(dictationRetry.Passed);
        Assert.True(dictationRetry.Gamification.AlreadyProcessed);
        Assert.Equal(4, dictationRetry.Gamification.Balance.Hearts);

        var passedDictationResponse = await PostPracticeAnswerAsync(
            client,
            lessonId,
            sentenceId,
            PracticeTabs.Dictation,
            "A VERIFIED production attempt!",
            "dictation-attempt-0002");
        var passedDictation = await passedDictationResponse.Content
            .ReadFromJsonAsync<PracticeAnswerEvaluationDto>();
        Assert.NotNull(passedDictation);
        Assert.True(passedDictation.Passed);
        Assert.Equal(20, passedDictation.Gamification.Delta.Exp);
        Assert.Equal(40, passedDictation.Gamification.Balance.Exp);

        var failedIpaResponse = await PostPracticeAnswerAsync(
            client,
            lessonId,
            sentenceId,
            PracticeTabs.IpaMatch,
            "/wrong/",
            "ipa-attempt-0001");
        var failedIpa = await failedIpaResponse.Content
            .ReadFromJsonAsync<PracticeAnswerEvaluationDto>();
        Assert.NotNull(failedIpa);
        Assert.False(failedIpa.Passed);
        Assert.Equal(-1, failedIpa.Gamification.Delta.Hearts);
        Assert.Equal(3, failedIpa.Gamification.Balance.Hearts);

        var passedIpaResponse = await PostPracticeAnswerAsync(
            client,
            lessonId,
            sentenceId,
            PracticeTabs.IpaMatch,
            "[ə ˈverɪfaɪd prəˈdʌkʃən əˈtempt]",
            "ipa-attempt-0002");
        var passedIpa = await passedIpaResponse.Content
            .ReadFromJsonAsync<PracticeAnswerEvaluationDto>();
        Assert.NotNull(passedIpa);
        Assert.True(passedIpa.Passed);
        Assert.Equal(20, passedIpa.Gamification.Delta.Exp);
        Assert.Equal(60, passedIpa.Gamification.Balance.Exp);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await verifyDb.PracticeAttempts.CountAsync(attempt =>
            attempt.UserId == userId
            && attempt.IdempotencyKey == idempotencyKey));
        Assert.Equal(3, await verifyDb.GamificationLedger.CountAsync(entry =>
            entry.UserId == userId
            && entry.SourceType == GamificationSourceTypes.SentenceCompletion));
        Assert.Equal(2, await verifyDb.GamificationLedger.CountAsync(entry =>
            entry.UserId == userId
            && entry.SourceType == GamificationSourceTypes.AttemptPenalty));
        Assert.Equal(1, await verifyDb.WordErrorStatistics.CountAsync(entry =>
            entry.UserId == userId
            && entry.NormalizedWord == "verified"));
    }

    private static async Task<HttpResponseMessage> PostShadowingAsync(
        HttpClient client,
        long lessonId,
        long sentenceId,
        string idempotencyKey)
    {
        var audio = new byte[44];
        "RIFF"u8.CopyTo(audio);
        "WAVE"u8.CopyTo(audio.AsSpan(8));

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(lessonId.ToString()), "lessonId");
        content.Add(new StringContent(sentenceId.ToString()), "sentenceId");
        content.Add(new StringContent("0"), "sentenceIndex");
        var audioContent = new ByteArrayContent(audio);
        audioContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, "audio", "attempt.wav");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/practice/evaluate-shadowing")
        {
            Content = content
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostPracticeAnswerAsync(
        HttpClient client,
        long lessonId,
        long sentenceId,
        string practiceTab,
        string answer,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/practice/evaluate-answer")
        {
            Content = JsonContent.Create(new PracticeAnswerRequestDto
            {
                LessonId = lessonId,
                SentenceId = sentenceId,
                PracticeTab = practiceTab,
                Answer = answer
            })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields)
    {
        return new FormUrlEncodedContent(fields.Select(field =>
            new KeyValuePair<string, string>(field.Key, field.Value)));
    }

    private static async Task<string> ReadAntiForgeryTokenAsync(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, html);
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, "The response did not contain an anti-forgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}

internal sealed class AuthFlowApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionStringEnvironmentVariable = "WEBSHADOWING_TEST_SQLSERVER";
    private readonly string _connectionString;
    private readonly bool _vipStubEnabled;
    private readonly Action<IServiceCollection>? _configureServices;
    private bool _databaseCreated;

    public AuthFlowApplicationFactory(
        bool vipStubEnabled = true,
        Action<IServiceCollection>? configureServices = null)
    {
        _vipStubEnabled = vipStubEnabled;
        _configureServices = configureServices;

        var configuredConnectionString = Environment.GetEnvironmentVariable(
            TestConnectionStringEnvironmentVariable)
            ?? "Server=localhost;Database=EnglishShadowingDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True";
        var connectionBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = $"EnglishShadowingDB_Test_{Guid.NewGuid():N}"
        };
        _connectionString = connectionBuilder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDatabaseProvider>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_connectionString));
            services.PostConfigure<VipStubOptions>(options => options.Enabled = _vipStubEnabled);
            _configureServices?.Invoke(services);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        _databaseCreated = true;
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _databaseCreated)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_connectionString)
                .Options;
            using var db = new AppDbContext(options);
            db.Database.EnsureDeleted();
        }
    }
}

internal sealed class SuccessfulPronunciationAssessmentService
    : IPronunciationAssessmentService
{
    public Task<PronunciationAssessmentResult> AssessAsync(
        PronunciationAssessmentRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PronunciationAssessmentResult
        {
            Provider = "deterministic-test-provider",
            ProviderReferenceId = "provider-attempt-1",
            OverallScore = 95,
            AccuracyScore = 95,
            FluencyScore = 95,
            CompletenessScore = 95,
            ProsodyScore = 95,
            Transcript = request.TargetText,
            Feedback = "Passed.",
            Words =
            [
                new PronunciationWordResult
                {
                    Word = "verified",
                    AccuracyCode = "correct"
                }
            ]
        });
}
