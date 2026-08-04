using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class GamificationService : IGamificationService
{
    private static readonly Regex WordNormalizeRegex = new(
        "[^\\p{L}\\p{Nd}']+",
        RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly GamificationOptions _options;
    private readonly VocabularyOptions _vocabularyOptions;
    private readonly ILanguageReferenceService _languageReferenceService;
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _businessTimeZone;

    public GamificationService(
        AppDbContext db,
        IOptions<GamificationOptions> options,
        IOptions<VocabularyOptions> vocabularyOptions,
        ILanguageReferenceService languageReferenceService,
        TimeProvider timeProvider)
    {
        _db = db;
        _options = options.Value;
        _vocabularyOptions = vocabularyOptions.Value;
        _languageReferenceService = languageReferenceService;
        _timeProvider = timeProvider;
        _businessTimeZone = ResolveTimeZone(_options.BusinessTimeZone);
    }

    public async Task<GamificationTransactionDto> ProcessVerifiedAttemptAsync(
        VerifiedPracticeAttempt command,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var user = await LockUserAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Rejected("attempt", "user_not_found", "Không tìm thấy người dùng.", new());
        }

        var statistic = await LockOrCreateStatisticAsync(user, cancellationToken);
        var existingAttempt = await _db.PracticeAttempts
            .AsNoTracking()
            .Include(attempt => attempt.Sentence)
            .SingleOrDefaultAsync(
                attempt => attempt.UserId == command.UserId
                    && attempt.IdempotencyKey == command.IdempotencyKey,
                cancellationToken);

        if (existingAttempt is not null)
        {
            var balance = ToBalance(statistic, user.IsVip);
            var sameSource = existingAttempt.SentenceId == command.SentenceId
                && existingAttempt.Sentence?.LessonId == command.LessonId
                && existingAttempt.PracticeTab == command.PracticeTab
                && existingAttempt.ExerciseType == command.ExerciseType;

            await transaction.CommitAsync(cancellationToken);
            return sameSource
                ? new GamificationTransactionDto
                {
                    Succeeded = true,
                    Applied = false,
                    AlreadyProcessed = true,
                    TransactionType = "attempt",
                    Balance = balance
                }
                : Rejected(
                    "attempt",
                    "idempotency_conflict",
                    "Idempotency key đã được dùng cho một attempt khác.",
                    balance);
        }

        var sentence = await _db.LessonSentences
            .AsNoTracking()
            .Include(item => item.Lesson)
                .ThenInclude(item => item.Course)
            .SingleOrDefaultAsync(
                item => item.SentenceId == command.SentenceId
                    && item.LessonId == command.LessonId,
                cancellationToken);
        if (sentence is null)
        {
            return Rejected(
                "attempt",
                "invalid_sentence",
                "Câu luyện không thuộc bài học.",
                ToBalance(statistic, user.IsVip));
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var practiceAttempt = new PracticeAttempt
        {
            UserId = command.UserId,
            SentenceId = command.SentenceId,
            PracticeTab = command.PracticeTab,
            ExerciseType = command.ExerciseType,
            TargetScore = command.TargetScore,
            Score = command.Score,
            Result = command.Passed ? AttemptResults.Passed : AttemptResults.Failed,
            AssessmentProvider = command.AssessmentProvider,
            ProviderReferenceId = command.ProviderReferenceId,
            TranscriptText = command.TranscriptText,
            FeedbackText = command.FeedbackText,
            IdempotencyKey = command.IdempotencyKey,
            AttemptedAt = nowUtc
        };
        _db.PracticeAttempts.Add(practiceAttempt);
        var wordStatistics = await UpdateWordErrorStatisticsAsync(
            command.UserId,
            command.SentenceId,
            command.Words,
            nowUtc,
            cancellationToken);
        await UpsertVocabularyItemsAsync(
            command.UserId,
            user.Accent,
            sentence,
            wordStatistics,
            nowUtc,
            cancellationToken);

        var ledgerEntries = new List<GamificationLedgerEntry>();
        var expDelta = 0;
        var heartsDelta = 0;
        var oldStreak = statistic.StreakDays;

        if (command.Passed)
        {
            var completionSourceId = $"{command.LessonId}:{command.PracticeTab}:{command.SentenceId}";
            var alreadyRewarded = await LedgerExistsAsync(
                command.UserId,
                GamificationSourceTypes.SentenceCompletion,
                completionSourceId,
                cancellationToken);
            expDelta = GamificationPolicy.CalculateCompletionExp(
                command.Passed,
                alreadyRewarded,
                _options.SentenceCompletionExp);
            if (!alreadyRewarded)
            {
                statistic.Exp += expDelta;
                ledgerEntries.Add(NewLedger(
                    user,
                    practiceAttempt,
                    GamificationSourceTypes.SentenceCompletion,
                    completionSourceId,
                    "sentence_completed",
                    expDelta: expDelta,
                    createdAt: nowUtc));
            }
        }
        else if (ConsumesHeart(command.PracticeTab, command.ExerciseType))
        {
            heartsDelta = GamificationPolicy.CalculateHeartPenalty(
                statistic.Hearts,
                _options.FailedAttemptHeartCost,
                user.IsVip);
            statistic.Hearts += heartsDelta;
            ledgerEntries.Add(NewLedger(
                user,
                practiceAttempt,
                GamificationSourceTypes.AttemptPenalty,
                command.IdempotencyKey,
                user.IsVip ? "vip_infinite_hearts" : heartsDelta == 0 ? "no_hearts_remaining" : "attempt_failed",
                heartsDelta: heartsDelta,
                createdAt: nowUtc));
        }

        var businessDate = GetBusinessDate(nowUtc);
        var activitySourceId = businessDate.ToString("yyyy-MM-dd");
        var activityRecorded = await LedgerExistsAsync(
            command.UserId,
            GamificationSourceTypes.DailyActivity,
            activitySourceId,
            cancellationToken);
        if (!activityRecorded)
        {
            DateOnly? lastPracticeDate = statistic.LastPracticeAt is null
                ? null
                : GetBusinessDate(DateTime.SpecifyKind(statistic.LastPracticeAt.Value, DateTimeKind.Utc));
            statistic.StreakDays = GamificationPolicy.CalculateStreak(
                lastPracticeDate,
                businessDate,
                statistic.StreakDays);
            statistic.LastPracticeAt = nowUtc;
            ledgerEntries.Add(NewLedger(
                user,
                practiceAttempt,
                GamificationSourceTypes.DailyActivity,
                activitySourceId,
                "daily_practice",
                streakDelta: statistic.StreakDays - oldStreak,
                createdAt: nowUtc));
        }

        await UpdateProgressAsync(command, nowUtc, cancellationToken);

        foreach (var entry in ledgerEntries)
        {
            SetBalanceSnapshot(entry, statistic);
        }
        _db.GamificationLedger.AddRange(ledgerEntries);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new GamificationTransactionDto
        {
            Succeeded = true,
            Applied = true,
            TransactionType = "attempt",
            Delta = new GamificationDeltaDto
            {
                Exp = expDelta,
                Hearts = heartsDelta,
                StreakDays = statistic.StreakDays - oldStreak
            },
            Balance = ToBalance(statistic, user.IsVip)
        };
    }

    public async Task<GamificationTransactionDto> ExchangeHeartAsync(
        long userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var user = await LockUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Rejected("heart_exchange", "user_not_found", "Không tìm thấy người dùng.", new());
        }

        var statistic = await LockOrCreateStatisticAsync(user, cancellationToken);
        var alreadyProcessed = await LedgerExistsAsync(
            userId,
            GamificationSourceTypes.HeartExchange,
            idempotencyKey,
            cancellationToken);
        if (alreadyProcessed)
        {
            await transaction.CommitAsync(cancellationToken);
            return new GamificationTransactionDto
            {
                Succeeded = true,
                Applied = false,
                AlreadyProcessed = true,
                TransactionType = "heart_exchange",
                Balance = ToBalance(statistic, user.IsVip)
            };
        }

        var balance = ToBalance(statistic, user.IsVip);
        if (user.IsVip)
        {
            return Rejected("heart_exchange", "vip_infinite_hearts", "Tài khoản VIP đã có tim vô hạn.", balance);
        }
        if (statistic.Hearts + _options.HeartExchangeAmount > _options.MaxHearts)
        {
            return Rejected("heart_exchange", "max_hearts", "Số tim đã đạt giới hạn.", balance);
        }
        if (statistic.Exp < _options.HeartExchangeExpCost)
        {
            return Rejected("heart_exchange", "insufficient_exp", "Không đủ EXP để đổi tim.", balance);
        }

        statistic.Exp -= _options.HeartExchangeExpCost;
        statistic.Hearts += _options.HeartExchangeAmount;
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var ledger = NewLedger(
            user,
            attempt: null,
            GamificationSourceTypes.HeartExchange,
            idempotencyKey,
            "exp_for_hearts",
            expDelta: -_options.HeartExchangeExpCost,
            heartsDelta: _options.HeartExchangeAmount,
            createdAt: nowUtc);
        SetBalanceSnapshot(ledger, statistic);
        _db.GamificationLedger.Add(ledger);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new GamificationTransactionDto
        {
            Succeeded = true,
            Applied = true,
            TransactionType = "heart_exchange",
            Delta = new GamificationDeltaDto
            {
                Exp = -_options.HeartExchangeExpCost,
                Hearts = _options.HeartExchangeAmount
            },
            Balance = ToBalance(statistic, user.IsVip)
        };
    }

    public Task<GamificationBalanceDto?> GetBalanceAsync(
        long userId,
        CancellationToken cancellationToken = default) =>
        _db.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId)
            .Select(user => new GamificationBalanceDto
            {
                IsVip = user.IsVip,
                Exp = user.Statistics == null ? 0 : user.Statistics.Exp,
                Hearts = user.Statistics == null ? 0 : user.Statistics.Hearts,
                StreakDays = user.Statistics == null ? 0 : user.Statistics.StreakDays
            })
            .SingleOrDefaultAsync(cancellationToken);

    private Task<User?> LockUserAsync(long userId, CancellationToken cancellationToken) =>
        _db.Users
            .FromSqlInterpolated($"SELECT * FROM dbo.Users WITH (UPDLOCK, ROWLOCK) WHERE user_id = {userId}")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<UserStatistic> LockOrCreateStatisticAsync(User user, CancellationToken cancellationToken)
    {
        var statistic = await _db.UserStatistics
            .FromSqlInterpolated($"SELECT * FROM dbo.User_Statistics WITH (UPDLOCK, ROWLOCK) WHERE user_id = {user.UserId}")
            .SingleOrDefaultAsync(cancellationToken);
        if (statistic is not null)
        {
            return statistic;
        }

        statistic = new UserStatistic
        {
            UserId = user.UserId,
            Hearts = _options.MaxHearts,
            Exp = 0,
            StreakDays = 0
        };
        _db.UserStatistics.Add(statistic);
        return statistic;
    }

    private Task<bool> LedgerExistsAsync(
        long userId,
        string sourceType,
        string sourceId,
        CancellationToken cancellationToken) =>
        _db.GamificationLedger.AnyAsync(
            entry => entry.UserId == userId
                && entry.SourceType == sourceType
                && entry.SourceId == sourceId,
            cancellationToken);

    private async Task<IReadOnlyList<WordErrorUpdate>> UpdateWordErrorStatisticsAsync(
        long userId,
        long sentenceId,
        IReadOnlyList<VerifiedPracticeWord> words,
        DateTime attemptedAt,
        CancellationToken cancellationToken)
    {
        if (words.Count == 0)
        {
            return [];
        }

        var grouped = words
            .Select(item => new
            {
                item.Word,
                item.AccuracyCode,
                Normalized = NormalizeWord(item.Word)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Normalized))
            .GroupBy(item => item.Normalized!, StringComparer.Ordinal)
            .Select(group => new
            {
                Normalized = group.Key,
                Display = group.Select(item => item.Word)
                    .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? group.Key,
                HasError = group.Any(item =>
                    item.AccuracyCode is "incorrect" or "warning")
            })
            .ToList();

        if (grouped.Count == 0)
        {
            return [];
        }

        var normalizedWords = grouped.Select(item => item.Normalized).ToList();
        var currentStats = await _db.WordErrorStatistics
            .Where(item =>
                item.UserId == userId
                && normalizedWords.Contains(item.NormalizedWord))
            .ToDictionaryAsync(
                item => item.NormalizedWord,
                StringComparer.Ordinal,
                cancellationToken);

        var updates = new List<WordErrorUpdate>(grouped.Count);
        foreach (var item in grouped)
        {
            if (!currentStats.TryGetValue(item.Normalized, out var statistic))
            {
                statistic = new WordErrorStatistic
                {
                    UserId = userId,
                    NormalizedWord = item.Normalized,
                    DisplayWord = item.Display
                };
                _db.WordErrorStatistics.Add(statistic);
                currentStats[item.Normalized] = statistic;
            }

            statistic.DisplayWord = item.Display;
            statistic.LastAttemptedAt = attemptedAt;
            statistic.LastSentenceId = sentenceId;
            statistic.UpdatedAt = attemptedAt;
            var previousConsecutiveCount = statistic.ConsecutiveErrorCount;

            if (item.HasError)
            {
                statistic.ConsecutiveErrorCount++;
                statistic.TotalErrorCount++;
                statistic.LastErrorAt = attemptedAt;
            }
            else
            {
                statistic.ConsecutiveErrorCount = 0;
            }

            updates.Add(new WordErrorUpdate(
                item.Normalized,
                item.Display,
                item.HasError,
                previousConsecutiveCount,
                statistic.ConsecutiveErrorCount));
        }

        return updates;
    }

    private async Task UpsertVocabularyItemsAsync(
        long userId,
        string accent,
        LessonSentence sentence,
        IReadOnlyList<WordErrorUpdate> wordStatistics,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var candidates = wordStatistics
            .Where(item => item.HasError && item.CurrentConsecutiveCount > _vocabularyOptions.WordErrorThreshold)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var existingItems = await _db.VocabularyItems
            .Where(item => item.UserId == userId
                && item.LanguageCode == "en"
                && candidates.Select(candidate => candidate.NormalizedWord).Contains(item.NormalizedWord))
            .ToDictionaryAsync(item => item.NormalizedWord, StringComparer.Ordinal, cancellationToken);

        var ipaEntries = await _languageReferenceService.GetIpaBatchAsync(
            candidates.Select(item => item.DisplayWord).ToList(),
            accent,
            cancellationToken);
        var ipaByWord = ipaEntries
            .Where(item => !string.IsNullOrWhiteSpace(item.Word))
            .GroupBy(item => NormalizeWord(item.Word) ?? string.Empty, StringComparer.Ordinal)
            .Where(group => group.Key.Length > 0)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Ipa).FirstOrDefault(ipa => !string.IsNullOrWhiteSpace(ipa)),
                StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            var meaning = await _languageReferenceService.GetMeaningAsync(
                candidate.DisplayWord,
                sentence.Text,
                cancellationToken);

            if (!existingItems.TryGetValue(candidate.NormalizedWord, out var item))
            {
                item = new VocabularyItem
                {
                    UserId = userId,
                    NormalizedWord = candidate.NormalizedWord,
                    LanguageCode = "en"
                };
                _db.VocabularyItems.Add(item);
                existingItems[candidate.NormalizedWord] = item;
            }

            item.DisplayWord = candidate.DisplayWord;
            item.Ipa = FirstNonEmpty(meaning.Ipa, ipaByWord.GetValueOrDefault(candidate.NormalizedWord), item.Ipa);
            item.Meaning = FirstNonEmpty(meaning.Meaning, item.Meaning);
            item.ExampleSentence = sentence.Text;
            item.SourceSentenceId = sentence.SentenceId;
            item.SourceType = VocabularySourceTypes.LessonSentence;
            item.SourceLessonId = sentence.LessonId;
            item.SourceLessonTitle = sentence.Lesson.Title;
            item.SourceSentenceText = sentence.Text;
            item.SourceLearningMode = sentence.Lesson.Course.LearningMode;
            item.UpdatedAt = nowUtc;
        }
    }

    private async Task UpdateProgressAsync(
        VerifiedPracticeAttempt command,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var sentenceProgress = await _db.UserSentenceProgress.SingleOrDefaultAsync(
            progress => progress.UserId == command.UserId
                && progress.SentenceId == command.SentenceId
                && progress.PracticeTab == command.PracticeTab,
            cancellationToken);
        var newlyCompleted = command.Passed
            && sentenceProgress?.Status != ProgressStatuses.Completed;

        if (sentenceProgress is null)
        {
            sentenceProgress = new UserSentenceProgress
            {
                UserId = command.UserId,
                SentenceId = command.SentenceId,
                PracticeTab = command.PracticeTab,
                Status = command.Passed ? ProgressStatuses.Completed : ProgressStatuses.InProgress,
                BestScore = command.Score,
                AttemptCount = 1,
                LastAttemptAt = nowUtc,
                CompletedAt = command.Passed ? nowUtc : null,
                UpdatedAt = nowUtc
            };
            _db.UserSentenceProgress.Add(sentenceProgress);
        }
        else
        {
            sentenceProgress.AttemptCount++;
            sentenceProgress.LastAttemptAt = nowUtc;
            sentenceProgress.BestScore = Math.Max(sentenceProgress.BestScore ?? 0, command.Score);
            sentenceProgress.UpdatedAt = nowUtc;
            if (command.Passed)
            {
                sentenceProgress.Status = ProgressStatuses.Completed;
                sentenceProgress.CompletedAt ??= nowUtc;
            }
            else if (sentenceProgress.Status == ProgressStatuses.NotStarted)
            {
                sentenceProgress.Status = ProgressStatuses.InProgress;
            }
        }

        var lessonProgress = await _db.UserLessonProgress.SingleOrDefaultAsync(
            progress => progress.UserId == command.UserId
                && progress.LessonId == command.LessonId
                && progress.PracticeTab == command.PracticeTab,
            cancellationToken);
        if (lessonProgress is null)
        {
            lessonProgress = new UserLessonProgress
            {
                UserId = command.UserId,
                LessonId = command.LessonId,
                PracticeTab = command.PracticeTab,
                StartedAt = nowUtc
            };
            _db.UserLessonProgress.Add(lessonProgress);
        }

        var completedCount = await _db.UserSentenceProgress.CountAsync(
            progress => progress.UserId == command.UserId
                && progress.PracticeTab == command.PracticeTab
                && progress.Status == ProgressStatuses.Completed
                && progress.Sentence.LessonId == command.LessonId,
            cancellationToken);
        if (newlyCompleted)
        {
            completedCount++;
        }
        var totalCount = await _db.LessonSentences.CountAsync(
            sentence => sentence.LessonId == command.LessonId,
            cancellationToken);

        lessonProgress.CurrentSentenceId = command.SentenceId;
        lessonProgress.CompletedSentenceCount = completedCount;
        lessonProgress.ProgressPercent = totalCount == 0
            ? 0
            : Math.Min(100m, decimal.Round(completedCount * 100m / totalCount, 2));
        lessonProgress.Status = completedCount >= totalCount && totalCount > 0
            ? ProgressStatuses.Completed
            : ProgressStatuses.InProgress;
        lessonProgress.CompletedAt = lessonProgress.Status == ProgressStatuses.Completed
            ? lessonProgress.CompletedAt ?? nowUtc
            : null;
        lessonProgress.UpdatedAt = nowUtc;
    }

    private DateOnly GetBusinessDate(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, _businessTimeZone));
    }

    private static string? NormalizeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        var normalized = WordNormalizeRegex.Replace(
            word.Trim().ToLowerInvariant(),
            string.Empty);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool ConsumesHeart(string practiceTab, string exerciseType) =>
        practiceTab is PracticeTabs.Shadowing or PracticeTabs.Dictation or PracticeTabs.IpaMatch
        || exerciseType is ExerciseTypes.Shadowing or ExerciseTypes.Pronunciation
            or ExerciseTypes.Dictation or ExerciseTypes.IpaMatch;

    private static GamificationLedgerEntry NewLedger(
        User user,
        PracticeAttempt? attempt,
        string sourceType,
        string sourceId,
        string reason,
        int expDelta = 0,
        int heartsDelta = 0,
        int streakDelta = 0,
        DateTime createdAt = default) => new()
        {
            UserId = user.UserId,
            User = user,
            Attempt = attempt,
            SourceType = sourceType,
            SourceId = sourceId,
            Reason = reason,
            ExpDelta = expDelta,
            HeartsDelta = heartsDelta,
            StreakDelta = streakDelta,
            IsVip = user.IsVip,
            CreatedAt = createdAt
        };

    private static void SetBalanceSnapshot(GamificationLedgerEntry entry, UserStatistic statistic)
    {
        entry.ExpBalance = statistic.Exp;
        entry.HeartsBalance = statistic.Hearts;
        entry.StreakBalance = statistic.StreakDays;
    }

    private static GamificationBalanceDto ToBalance(UserStatistic statistic, bool isVip) => new()
    {
        Exp = statistic.Exp,
        Hearts = statistic.Hearts,
        StreakDays = statistic.StreakDays,
        IsVip = isVip
    };

    private static GamificationTransactionDto Rejected(
        string transactionType,
        string code,
        string message,
        GamificationBalanceDto balance) => new()
        {
            Succeeded = false,
            Applied = false,
            TransactionType = transactionType,
            RejectionCode = code,
            Message = message,
            Balance = balance
        };

    private static TimeZoneInfo ResolveTimeZone(string configuredId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(configuredId);
        }
        catch (TimeZoneNotFoundException)
        {
            var fallback = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
            return TimeZoneInfo.FindSystemTimeZoneById(fallback);
        }
    }

    private sealed record WordErrorUpdate(
        string NormalizedWord,
        string DisplayWord,
        bool HasError,
        int PreviousConsecutiveCount,
        int CurrentConsecutiveCount);
}
