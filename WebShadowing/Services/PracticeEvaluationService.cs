using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class PracticeEvaluationService : IPracticeEvaluationService
{
    private static readonly Regex WordNormalizeRegex = new("[^\\p{L}\\p{Nd}']+", RegexOptions.Compiled);
    private readonly AppDbContext _db;
    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly IPronunciationAssessmentService _assessmentService;
    private readonly PronunciationScoreProfileService _scoreProfileService;
    private readonly PronunciationAssessmentOptions _assessmentOptions;

    public PracticeEvaluationService(
        AppDbContext db,
        ICourseService courseService,
        IUserContextService userContext,
        IPronunciationAssessmentService assessmentService,
        PronunciationScoreProfileService scoreProfileService,
        IOptions<PronunciationAssessmentOptions> assessmentOptions)
    {
        _db = db;
        _courseService = courseService;
        _userContext = userContext;
        _assessmentService = assessmentService;
        _scoreProfileService = scoreProfileService;
        _assessmentOptions = assessmentOptions.Value;
    }

    public async Task<ShadowingEvaluationDto> EvaluateAsync(
        EvaluateShadowingCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Không xác định được người dùng hiện tại.",
                StatusCodes.Status401Unauthorized,
                "unauthorized");
        }

        var idempotencyKey = (command.IdempotencyKey ?? string.Empty).Trim();
        if (idempotencyKey.Length == 0 || idempotencyKey.Length > 100)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Thiếu hoặc sai định dạng idempotency key.",
                StatusCodes.Status400BadRequest,
                "invalid_idempotency_key");
        }

        var cachedAttempt = await _db.PracticeAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId.Value && item.IdempotencyKey == idempotencyKey, cancellationToken);
        if (cachedAttempt is not null)
        {
            var cachedScore = cachedAttempt.Score.HasValue ? (int)Math.Round(cachedAttempt.Score.Value, MidpointRounding.AwayFromZero) : 0;
            return new ShadowingEvaluationDto
            {
                Score = Math.Clamp(cachedScore, 0, 100),
                Passed = string.Equals(cachedAttempt.Result, AttemptResults.Passed, StringComparison.OrdinalIgnoreCase),
                PronunciationTarget = (byte)Math.Clamp(cachedAttempt.TargetScore, 0, 100),
                Provider = cachedAttempt.AssessmentProvider ?? "unknown",
                Transcript = cachedAttempt.TranscriptText ?? string.Empty,
                Feedback = cachedAttempt.FeedbackText ?? "",
                Words = []
            };
        }

        ValidateAudio(command.Audio, command.AudioFormat, command.ContentType, _assessmentOptions.MaxAudioDurationSeconds);

        var learningMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var accent = await _userContext.GetAccentAsync(cancellationToken);
        var pronunciationTarget = await _userContext.GetPronunciationTargetAsync(cancellationToken);
        var lessonResult = await _courseService.GetLessonAsync(
            command.LessonId,
            learningMode,
            pronunciationTarget,
            cancellationToken);

        if (lessonResult.Status == LessonLookupStatus.Forbidden)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Bài học không thuộc chế độ luyện hiện tại của bạn.",
                StatusCodes.Status403Forbidden,
                "lesson_mode_forbidden");
        }

        if (lessonResult.Lesson is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Không tìm thấy bài học luyện phát âm.",
                StatusCodes.Status404NotFound,
                "lesson_not_found");
        }

        var sentence = lessonResult.Lesson.Sentences.FirstOrDefault(item => item.SentenceId == command.SentenceId)
            ?? lessonResult.Lesson.Sentences.ElementAtOrDefault(command.SentenceIndex);
        if (sentence is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Câu luyện không hợp lệ.",
                StatusCodes.Status400BadRequest,
                "invalid_sentence");
        }

        var providerResult = await _assessmentService.AssessAsync(
            new PronunciationAssessmentRequest(
                command.Audio,
                command.AudioFormat,
                accent,
                learningMode,
                sentence.Text,
                sentence.Ipa,
                pronunciationTarget),
            cancellationToken);

        var score = _scoreProfileService.ComputeOverallScore(learningMode, providerResult);
        var passed = score >= pronunciationTarget;

        await PersistAttemptAsync(
            userId.Value,
            sentence.SentenceId,
            idempotencyKey,
            pronunciationTarget,
            providerResult,
            score,
            passed,
            cancellationToken);

        return MapToDto(providerResult, score, passed, pronunciationTarget, accent, learningMode);
    }

    private async Task PersistAttemptAsync(
        long userId,
        long sentenceId,
        string idempotencyKey,
        byte pronunciationTarget,
        PronunciationAssessmentResult providerResult,
        int score,
        bool passed,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        _db.PracticeAttempts.Add(new PracticeAttempt
        {
            UserId = userId,
            SentenceId = sentenceId,
            PracticeTab = PracticeTabs.Shadowing,
            ExerciseType = ExerciseTypes.Pronunciation,
            TargetScore = pronunciationTarget,
            Score = score,
            Result = passed ? AttemptResults.Passed : AttemptResults.Failed,
            AssessmentProvider = providerResult.Provider,
            ProviderReferenceId = providerResult.ProviderReferenceId,
            TranscriptText = providerResult.Transcript,
            FeedbackText = providerResult.Feedback,
            IdempotencyKey = idempotencyKey,
            AttemptedAt = now
        });

        await UpdateWordErrorStatisticsAsync(userId, sentenceId, providerResult.Words, now, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent retry can race this insert. Surface the already-persisted attempt as idempotent success.
            var existing = await _db.PracticeAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == userId && item.IdempotencyKey == idempotencyKey, cancellationToken);
            if (existing is null)
            {
                throw;
            }
        }
    }

    private async Task UpdateWordErrorStatisticsAsync(
        long userId,
        long sentenceId,
        IReadOnlyList<PronunciationWordResult> words,
        DateTime attemptedAt,
        CancellationToken cancellationToken)
    {
        if (words.Count == 0)
        {
            return;
        }

        var grouped = words
            .Select(item => new { item.Word, item.AccuracyCode, Normalized = NormalizeWord(item.Word) })
            .Where(item => !string.IsNullOrWhiteSpace(item.Normalized))
            .GroupBy(item => item.Normalized!, StringComparer.Ordinal)
            .Select(group => new
            {
                Normalized = group.Key,
                Display = group.Select(item => item.Word).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? group.Key,
                HasError = group.Any(item => item.AccuracyCode is "incorrect" or "warning")
            })
            .ToList();

        if (grouped.Count == 0)
        {
            return;
        }

        var normalizedWords = grouped.Select(item => item.Normalized).ToList();
        var currentStats = await _db.WordErrorStatistics
            .Where(item => item.UserId == userId && normalizedWords.Contains(item.NormalizedWord))
            .ToDictionaryAsync(item => item.NormalizedWord, StringComparer.Ordinal, cancellationToken);

        foreach (var item in grouped)
        {
            if (!currentStats.TryGetValue(item.Normalized, out var stat))
            {
                stat = new WordErrorStatistic
                {
                    UserId = userId,
                    NormalizedWord = item.Normalized,
                    DisplayWord = item.Display,
                    ConsecutiveErrorCount = 0,
                    TotalErrorCount = 0
                };
                _db.WordErrorStatistics.Add(stat);
                currentStats[item.Normalized] = stat;
            }

            stat.DisplayWord = item.Display;
            stat.LastAttemptedAt = attemptedAt;
            stat.LastSentenceId = sentenceId;
            stat.UpdatedAt = attemptedAt;

            if (item.HasError)
            {
                stat.ConsecutiveErrorCount += 1;
                stat.TotalErrorCount += 1;
                stat.LastErrorAt = attemptedAt;
            }
            else
            {
                stat.ConsecutiveErrorCount = 0;
            }
        }
    }

    private static ShadowingEvaluationDto MapToDto(
        PronunciationAssessmentResult result,
        int score,
        bool passed,
        byte pronunciationTarget,
        string accent,
        string learningMode)
    {
        return new ShadowingEvaluationDto
        {
            Score = Math.Clamp(score, 0, 100),
            Passed = passed,
            PronunciationTarget = pronunciationTarget,
            Provider = result.Provider,
            Accent = accent,
            LearningMode = learningMode,
            AccuracyScore = result.AccuracyScore,
            FluencyScore = result.FluencyScore,
            CompletenessScore = result.CompletenessScore,
            ProsodyScore = result.ProsodyScore,
            Transcript = result.Transcript,
            Feedback = result.Feedback,
            Words = result.Words.Select(item => new WordFeedbackDto
            {
                Word = item.Word,
                AccuracyCode = item.AccuracyCode,
                Correction = item.Correction,
                Phonemes = item.Phonemes.Select(phoneme => new PhonemeFeedbackDto
                {
                    Symbol = phoneme.Symbol,
                    AccuracyCode = phoneme.AccuracyCode
                }).ToList()
            }).ToList()
        };
    }

    private static void ValidateAudio(byte[] audio, string audioFormat, string contentType, int maxAudioDurationSeconds)
    {
        if (audio.Length == 0)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Không nhận được file thu âm.",
                StatusCodes.Status400BadRequest,
                "empty_audio");
        }

        if (audio.Length > 10 * 1024 * 1024)
        {
            throw new PronunciationAssessmentUnavailableException(
                "File thu âm vượt quá giới hạn 10 MB.",
                StatusCodes.Status400BadRequest,
                "audio_too_large");
        }

        var normalizedFormat = (audioFormat ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedFormat is not "wav" and not "mp3")
        {
            throw new PronunciationAssessmentUnavailableException(
                "Định dạng audio không được hỗ trợ. Chỉ chấp nhận WAV hoặc MP3.",
                StatusCodes.Status400BadRequest,
                "unsupported_audio_format");
        }

        if (!string.IsNullOrWhiteSpace(contentType)
            && !contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            throw new PronunciationAssessmentUnavailableException(
                "MIME type của file thu âm không hợp lệ.",
                StatusCodes.Status400BadRequest,
                "invalid_audio_mime");
        }

        if (normalizedFormat == "wav")
        {
            var durationSeconds = TryGetWavDurationSeconds(audio);
            if (durationSeconds is not null && durationSeconds > maxAudioDurationSeconds)
            {
                throw new PronunciationAssessmentUnavailableException(
                    $"Thời lượng thu âm vượt quá giới hạn {maxAudioDurationSeconds} giây.",
                    StatusCodes.Status400BadRequest,
                    "audio_duration_exceeded");
            }
        }
    }

    private static double? TryGetWavDurationSeconds(byte[] audio)
    {
        if (audio.Length < 44)
        {
            return null;
        }

        if (audio[0] != 'R' || audio[1] != 'I' || audio[2] != 'F' || audio[3] != 'F')
        {
            return null;
        }

        var byteRate = BitConverter.ToInt32(audio, 28);
        var dataSize = BitConverter.ToInt32(audio, 40);
        if (byteRate <= 0 || dataSize < 0)
        {
            return null;
        }

        return dataSize / (double)byteRate;
    }

    private static string? NormalizeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        var lowered = word.Trim().ToLowerInvariant();
        var normalized = WordNormalizeRegex.Replace(lowered, string.Empty);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
