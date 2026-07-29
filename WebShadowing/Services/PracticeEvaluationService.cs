using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class PracticeEvaluationService : IPracticeEvaluationService
{
    private readonly AppDbContext _db;
    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly IPronunciationAssessmentService _assessmentService;
    private readonly IGamificationService _gamificationService;
    private readonly PronunciationScoreProfileService _scoreProfileService;
    private readonly PronunciationAssessmentOptions _assessmentOptions;

    public PracticeEvaluationService(
        AppDbContext db,
        ICourseService courseService,
        IUserContextService userContext,
        IPronunciationAssessmentService assessmentService,
        IGamificationService gamificationService,
        PronunciationScoreProfileService scoreProfileService,
        IOptions<PronunciationAssessmentOptions> assessmentOptions)
    {
        _db = db;
        _courseService = courseService;
        _userContext = userContext;
        _assessmentService = assessmentService;
        _gamificationService = gamificationService;
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
            .Include(attempt => attempt.Sentence)
            .FirstOrDefaultAsync(item => item.UserId == userId.Value && item.IdempotencyKey == idempotencyKey, cancellationToken);
        if (cachedAttempt is not null)
        {
            var sameSource = cachedAttempt.SentenceId == command.SentenceId
                && cachedAttempt.Sentence?.LessonId == command.LessonId
                && cachedAttempt.PracticeTab == PracticeTabs.Shadowing
                && cachedAttempt.ExerciseType == ExerciseTypes.Pronunciation;
            if (!sameSource)
            {
                throw new PronunciationAssessmentUnavailableException(
                    "Idempotency-Key đã được sử dụng cho một attempt khác.",
                    StatusCodes.Status409Conflict,
                    "idempotency_conflict");
            }

            var balance = await _gamificationService.GetBalanceAsync(
                userId.Value,
                cancellationToken);
            if (balance is null)
            {
                throw new PronunciationAssessmentUnavailableException(
                    "Không tìm thấy người dùng.",
                    StatusCodes.Status404NotFound,
                    "user_not_found");
            }

            var cachedScore = cachedAttempt.Score.HasValue ? (int)Math.Round(cachedAttempt.Score.Value, MidpointRounding.AwayFromZero) : 0;
            return new ShadowingEvaluationDto
            {
                Score = Math.Clamp(cachedScore, 0, 100),
                Passed = string.Equals(cachedAttempt.Result, AttemptResults.Passed, StringComparison.OrdinalIgnoreCase),
                PronunciationTarget = (byte)Math.Clamp(cachedAttempt.TargetScore, 0, 100),
                Provider = cachedAttempt.AssessmentProvider ?? "unknown",
                Transcript = cachedAttempt.TranscriptText ?? string.Empty,
                Feedback = cachedAttempt.FeedbackText ?? "",
                Words = [],
                Gamification = new GamificationTransactionDto
                {
                    Succeeded = true,
                    Applied = false,
                    AlreadyProcessed = true,
                    TransactionType = "attempt",
                    Balance = balance
                }
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

        var gamification = await _gamificationService.ProcessVerifiedAttemptAsync(
            new VerifiedPracticeAttempt
            {
                UserId = userId.Value,
                LessonId = command.LessonId,
                SentenceId = sentence.SentenceId,
                PracticeTab = PracticeTabs.Shadowing,
                ExerciseType = ExerciseTypes.Pronunciation,
                TargetScore = pronunciationTarget,
                Score = score,
                Passed = passed,
                IdempotencyKey = idempotencyKey,
                AssessmentProvider = providerResult.Provider,
                ProviderReferenceId = providerResult.ProviderReferenceId,
                TranscriptText = providerResult.Transcript,
                FeedbackText = providerResult.Feedback,
                Words = providerResult.Words
                    .Select(word => new VerifiedPracticeWord(
                        word.Word,
                        word.AccuracyCode))
                    .ToList()
            },
            cancellationToken);
        EnsureGamificationSucceeded(gamification);

        return MapToDto(
            providerResult,
            score,
            passed,
            pronunciationTarget,
            accent,
            learningMode,
            gamification);
    }

    public async Task<PracticeAnswerEvaluationDto> EvaluateAnswerAsync(
        EvaluatePracticeAnswerCommand command,
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

        var exerciseType = command.PracticeTab switch
        {
            PracticeTabs.Dictation => ExerciseTypes.Dictation,
            PracticeTabs.IpaMatch => ExerciseTypes.IpaMatch,
            _ => throw new PronunciationAssessmentUnavailableException(
                "Chế độ luyện tập không được hỗ trợ.",
                StatusCodes.Status400BadRequest,
                "unsupported_practice_tab")
        };
        if (string.IsNullOrWhiteSpace(command.Answer)
            || command.Answer.Length > 4000)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Câu trả lời không hợp lệ.",
                StatusCodes.Status400BadRequest,
                "invalid_answer");
        }

        var learningMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var pronunciationTarget = await _userContext.GetPronunciationTargetAsync(cancellationToken);
        var lessonResult = await _courseService.GetLessonAsync(
            command.LessonId,
            learningMode,
            pronunciationTarget,
            cancellationToken);
        if (lessonResult.Status == LessonLookupStatus.Forbidden)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Bài học không thuộc chế độ luyện hiện tại.",
                StatusCodes.Status403Forbidden,
                "lesson_mode_forbidden");
        }

        var sentence = lessonResult.Lesson?.Sentences
            .SingleOrDefault(item => item.SentenceId == command.SentenceId);
        if (sentence is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Câu luyện không thuộc bài học.",
                StatusCodes.Status400BadRequest,
                "invalid_sentence");
        }

        var expectedAnswer = command.PracticeTab == PracticeTabs.Dictation
            ? sentence.Text
            : sentence.Ipa;
        if (string.IsNullOrWhiteSpace(expectedAnswer))
        {
            throw new PronunciationAssessmentUnavailableException(
                "Câu này chưa có dữ liệu IPA để chấm.",
                StatusCodes.Status409Conflict,
                "ipa_not_available");
        }

        var normalizedActual = command.PracticeTab == PracticeTabs.Dictation
            ? NormalizeDictation(command.Answer)
            : NormalizeIpa(command.Answer);
        var normalizedExpected = command.PracticeTab == PracticeTabs.Dictation
            ? NormalizeDictation(expectedAnswer)
            : NormalizeIpa(expectedAnswer);
        var passed = normalizedActual.Length > 0
            && string.Equals(
                normalizedActual,
                normalizedExpected,
                StringComparison.Ordinal);
        var score = passed ? 100 : 0;
        var feedback = passed
            ? "Câu trả lời chính xác."
            : command.PracticeTab == PracticeTabs.Dictation
                ? "Nội dung nghe chép chưa chính xác."
                : "Phiên âm IPA chưa chính xác.";

        var gamification = await _gamificationService.ProcessVerifiedAttemptAsync(
            new VerifiedPracticeAttempt
            {
                UserId = userId.Value,
                LessonId = command.LessonId,
                SentenceId = sentence.SentenceId,
                PracticeTab = command.PracticeTab,
                ExerciseType = exerciseType,
                TargetScore = 100,
                Score = score,
                Passed = passed,
                IdempotencyKey = idempotencyKey,
                AssessmentProvider = "server-answer-validator",
                TranscriptText = command.Answer.Trim(),
                FeedbackText = feedback
            },
            cancellationToken);
        EnsureGamificationSucceeded(gamification);

        if (gamification.AlreadyProcessed)
        {
            var persistedAttempt = await _db.PracticeAttempts
                .AsNoTracking()
                .SingleAsync(
                    attempt => attempt.UserId == userId.Value
                        && attempt.IdempotencyKey == idempotencyKey,
                    cancellationToken);
            score = persistedAttempt.Score.HasValue
                ? (int)Math.Round(
                    persistedAttempt.Score.Value,
                    MidpointRounding.AwayFromZero)
                : 0;
            passed = string.Equals(
                persistedAttempt.Result,
                AttemptResults.Passed,
                StringComparison.OrdinalIgnoreCase);
            feedback = persistedAttempt.FeedbackText ?? feedback;
        }

        return new PracticeAnswerEvaluationDto
        {
            Score = Math.Clamp(score, 0, 100),
            Passed = passed,
            Feedback = feedback,
            Gamification = gamification
        };
    }

    private static ShadowingEvaluationDto MapToDto(
        PronunciationAssessmentResult result,
        int score,
        bool passed,
        byte pronunciationTarget,
        string accent,
        string learningMode,
        GamificationTransactionDto gamification)
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
            Gamification = gamification,
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

    private static void EnsureGamificationSucceeded(
        GamificationTransactionDto transaction)
    {
        if (transaction.Succeeded)
        {
            return;
        }

        var statusCode = transaction.RejectionCode switch
        {
            "user_not_found" => StatusCodes.Status404NotFound,
            "idempotency_conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        throw new PronunciationAssessmentUnavailableException(
            transaction.Message ?? "Không thể cập nhật kết quả luyện tập.",
            statusCode,
            transaction.RejectionCode ?? "gamification_rejected");
    }

    private static string NormalizeDictation(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value.Normalize(NormalizationForm.FormKC)
                     .ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character == '\'')
            {
                if (pendingSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(character);
                pendingSpace = false;
            }
            else
            {
                pendingSpace = true;
            }
        }
        return builder.ToString();
    }

    private static string NormalizeIpa(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormKC)
                     .ToLowerInvariant())
        {
            if (!char.IsWhiteSpace(character)
                && character is not '/' and not '[' and not ']')
            {
                builder.Append(character);
            }
        }
        return builder.ToString();
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

}
