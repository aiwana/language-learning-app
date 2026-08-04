using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class IpaMatchService : IIpaMatchService
{
    private const int DesiredOptionCount = 4;
    private static readonly TimeSpan QuestionTtl = TimeSpan.FromMinutes(15);

    private static readonly HashSet<string> StopWords =
    [
        "a", "an", "the", "and", "or", "but", "to", "of", "for", "in", "on", "at", "by",
        "from", "with", "as", "is", "are", "was", "were", "be", "been", "being", "it", "this",
        "that", "these", "those", "i", "you", "he", "she", "we", "they", "me", "my", "your",
        "our", "their", "his", "her", "its", "am", "do", "does", "did", "have", "has", "had"
    ];

    private readonly AppDbContext _db;
    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly ILanguageReferenceService _languageReferenceService;
    private readonly IGamificationService _gamificationService;
    private readonly IMemoryCache _cache;
    private readonly IDataProtector _tokenProtector;

    public IpaMatchService(
        AppDbContext db,
        ICourseService courseService,
        IUserContextService userContext,
        ILanguageReferenceService languageReferenceService,
        IGamificationService gamificationService,
        IMemoryCache cache,
        IDataProtectionProvider dataProtectionProvider)
    {
        _db = db;
        _courseService = courseService;
        _userContext = userContext;
        _languageReferenceService = languageReferenceService;
        _gamificationService = gamificationService;
        _cache = cache;
        _tokenProtector = dataProtectionProvider.CreateProtector("WebShadowing.IpaMatchQuestion.v1");
    }

    public async Task<IpaMatchQuestionDto> GetQuestionAsync(
        GetIpaMatchQuestionCommand command,
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

        var accent = await _userContext.GetAccentAsync(cancellationToken);
        var sentence = await ResolveSentenceAsync(command.LessonId, command.SentenceId, cancellationToken);

        var latestKey = BuildLatestQuestionKey(userId.Value, command.LessonId, command.SentenceId, accent);
        if (_cache.TryGetValue<string>(latestKey, out var existingQuestionId)
            && TryGetSnapshot(existingQuestionId, out var existingSnapshot)
            && existingSnapshot.UserId == userId.Value
            && existingSnapshot.ExpiresAtUtc > DateTime.UtcNow)
        {
            return ToQuestionDto(existingSnapshot);
        }

        var question = await BuildQuestionAsync(
            userId.Value,
            command.LessonId,
            sentence,
            accent,
            cancellationToken);

        CacheSnapshot(question);
        _cache.Set(
            latestKey,
            question.QuestionId,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = question.ExpiresAtUtc,
                Size = 1
            });

        return ToQuestionDto(question);
    }

    public async Task<PracticeAnswerEvaluationDto> SubmitAnswerAsync(
        SubmitIpaMatchAnswerCommand command,
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

        var tokenPayload = ParseToken(command.QuestionToken);
        if (tokenPayload.UserId != userId.Value)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Question token không hợp lệ cho người dùng hiện tại.",
                StatusCodes.Status403Forbidden,
                "question_token_user_mismatch");
        }

        if (tokenPayload.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Question đã hết hạn. Vui lòng lấy câu mới.",
                StatusCodes.Status410Gone,
                "question_expired");
        }

        if (!TryGetSnapshot(tokenPayload.QuestionId, out var snapshot)
            || snapshot.UserId != userId.Value)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Question không còn khả dụng. Vui lòng lấy câu mới.",
                StatusCodes.Status410Gone,
                "question_not_found");
        }

        if (snapshot.Evaluation is not null)
        {
            return snapshot.Evaluation;
        }

        var selectedOption = snapshot.Options.SingleOrDefault(option =>
            string.Equals(option.OptionId, command.OptionId, StringComparison.Ordinal));
        if (selectedOption is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Lựa chọn IPA không hợp lệ.",
                StatusCodes.Status400BadRequest,
                "invalid_option");
        }

        var passed = string.Equals(
            selectedOption.OptionId,
            snapshot.CorrectOptionId,
            StringComparison.Ordinal);
        var score = passed ? 100 : 0;
        var feedback = passed
            ? "Chính xác. Bạn đã ghép đúng phiên âm."
            : "Chưa chính xác. Hãy nghe lại và thử câu tiếp theo.";

        var gamification = await _gamificationService.ProcessVerifiedAttemptAsync(
            new VerifiedPracticeAttempt
            {
                UserId = userId.Value,
                LessonId = snapshot.LessonId,
                SentenceId = snapshot.SentenceId,
                PracticeTab = PracticeTabs.IpaMatch,
                ExerciseType = ExerciseTypes.IpaMatch,
                TargetScore = 100,
                Score = score,
                Passed = passed,
                IdempotencyKey = snapshot.IdempotencyKey,
                AssessmentProvider = "ipa-match-question",
                TranscriptText = selectedOption.Ipa,
                FeedbackText = feedback
            },
            cancellationToken);

        EnsureGamificationSucceeded(gamification);

        if (gamification.AlreadyProcessed)
        {
            var persistedAttempt = await _db.PracticeAttempts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    attempt => attempt.UserId == userId.Value
                        && attempt.IdempotencyKey == snapshot.IdempotencyKey,
                    cancellationToken);

            if (persistedAttempt is not null)
            {
                score = persistedAttempt.Score.HasValue
                    ? (int)Math.Round(persistedAttempt.Score.Value, MidpointRounding.AwayFromZero)
                    : score;
                passed = string.Equals(
                    persistedAttempt.Result,
                    AttemptResults.Passed,
                    StringComparison.OrdinalIgnoreCase);
                feedback = persistedAttempt.FeedbackText ?? feedback;
            }
        }

        var evaluation = new PracticeAnswerEvaluationDto
        {
            Score = Math.Clamp(score, 0, 100),
            Passed = passed,
            Feedback = feedback,
            Gamification = gamification
        };

        snapshot.Evaluation = evaluation;
        CacheSnapshot(snapshot);

        return evaluation;
    }

    private async Task<LessonSentenceDto> ResolveSentenceAsync(
        long lessonId,
        long sentenceId,
        CancellationToken cancellationToken)
    {
        var learningMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var pronunciationTarget = await _userContext.GetPronunciationTargetAsync(cancellationToken);
        var lessonResult = await _courseService.GetLessonAsync(
            lessonId,
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

        if (lessonResult.Lesson is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Không tìm thấy bài học.",
                StatusCodes.Status404NotFound,
                "lesson_not_found");
        }

        var sentence = lessonResult.Lesson.Sentences.SingleOrDefault(item => item.SentenceId == sentenceId);
        if (sentence is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Câu luyện không thuộc bài học.",
                StatusCodes.Status400BadRequest,
                "invalid_sentence");
        }

        return sentence;
    }

    private async Task<IpaQuestionSnapshot> BuildQuestionAsync(
        long userId,
        long lessonId,
        LessonSentenceDto sentence,
        string accent,
        CancellationToken cancellationToken)
    {
        var promptCandidates = ExtractWords(sentence.Text)
            .Where(word => IsEligiblePromptWord(word.Raw, word.Normalized))
            .ToList();

        if (promptCandidates.Count == 0)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Không tìm thấy từ phù hợp để tạo câu hỏi IPA.",
                StatusCodes.Status409Conflict,
                "ipa_prompt_word_not_found");
        }

        var candidateWordList = promptCandidates
            .Select(word => word.Normalized)
            .Distinct(StringComparer.Ordinal)
            .Take(40)
            .ToList();

        var candidateIpas = await _languageReferenceService.GetIpaBatchAsync(
            candidateWordList,
            accent,
            cancellationToken);

        var candidateIpaMap = candidateIpas
            .GroupBy(item => item.Word.Trim().ToLowerInvariant(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => NormalizeIpaForDisplay(group.First().Ipa), StringComparer.Ordinal);

        var selectedPrompt = promptCandidates
            .Select(candidate => new
            {
                candidate.Raw,
                candidate.Normalized,
                Ipa = candidateIpaMap.GetValueOrDefault(candidate.Normalized)
            })
            .FirstOrDefault(item => IsValidIpa(item.Ipa));

        if (selectedPrompt is null)
        {
            throw new PronunciationAssessmentUnavailableException(
                "IPA provider chưa sẵn sàng cho câu này. Vui lòng thử lại sau.",
                StatusCodes.Status503ServiceUnavailable,
                "ipa_provider_unavailable");
        }

        var distractorWords = ExtractWords(sentence.Text)
            .Select(word => word.Normalized)
            .Where(word => !string.Equals(word, selectedPrompt.Normalized, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var distractorIpas = await _languageReferenceService.GetIpaBatchAsync(
            distractorWords,
            accent,
            cancellationToken);

        var correctIpa = selectedPrompt.Ipa!;
        var normalizedCorrectIpa = NormalizeIpa(correctIpa);

        var optionIpas = new List<string> { correctIpa };
        foreach (var distractor in distractorIpas)
        {
            var ipa = NormalizeIpaForDisplay(distractor.Ipa);
            if (!IsValidIpa(ipa))
            {
                continue;
            }

            var normalized = NormalizeIpa(ipa);
            if (normalized.Length == 0
                || string.Equals(normalized, normalizedCorrectIpa, StringComparison.Ordinal)
                || optionIpas.Any(existing => string.Equals(NormalizeIpa(existing), normalized, StringComparison.Ordinal)))
            {
                continue;
            }

            optionIpas.Add(ipa);
            if (optionIpas.Count == DesiredOptionCount)
            {
                break;
            }
        }

        while (optionIpas.Count < 3)
        {
            var generated = GenerateDistractorIpa(correctIpa, optionIpas.Count);
            if (!optionIpas.Any(existing =>
                string.Equals(NormalizeIpa(existing), NormalizeIpa(generated), StringComparison.Ordinal)))
            {
                optionIpas.Add(generated);
            }
        }

        while (optionIpas.Count < DesiredOptionCount)
        {
            var generated = GenerateDistractorIpa(correctIpa, optionIpas.Count + 3);
            if (!optionIpas.Any(existing =>
                string.Equals(NormalizeIpa(existing), NormalizeIpa(generated), StringComparison.Ordinal)))
            {
                optionIpas.Add(generated);
            }
            else
            {
                break;
            }
        }

        Shuffle(optionIpas);

        var options = optionIpas
            .Select(ipa => new IpaMatchOptionDto
            {
                OptionId = $"opt-{Guid.NewGuid():N}"[..12],
                Ipa = ipa
            })
            .ToList();

        var correctOption = options.Single(option =>
            string.Equals(NormalizeIpa(option.Ipa), normalizedCorrectIpa, StringComparison.Ordinal));

        var now = DateTime.UtcNow;
        var questionId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var expiresAtUtc = now.Add(QuestionTtl);

        return new IpaQuestionSnapshot
        {
            QuestionId = questionId,
            UserId = userId,
            LessonId = lessonId,
            SentenceId = sentence.SentenceId,
            Accent = accent,
            PromptWord = selectedPrompt.Raw,
            Options = options,
            CorrectOptionId = correctOption.OptionId,
            ExpiresAtUtc = expiresAtUtc,
            IdempotencyKey = $"ipa-question-{questionId}"
        };
    }

    private void CacheSnapshot(IpaQuestionSnapshot snapshot)
    {
        _cache.Set(
            BuildQuestionCacheKey(snapshot.QuestionId),
            snapshot,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = snapshot.ExpiresAtUtc,
                Size = 1
            });
    }

    private bool TryGetSnapshot(string? questionId, out IpaQuestionSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(questionId)
            && _cache.TryGetValue<IpaQuestionSnapshot>(BuildQuestionCacheKey(questionId), out var cached)
            && cached is not null)
        {
            snapshot = cached;
            return true;
        }

        snapshot = null!;
        return false;
    }

    private IpaMatchQuestionDto ToQuestionDto(IpaQuestionSnapshot snapshot)
    {
        var token = BuildToken(snapshot.QuestionId, snapshot.UserId, snapshot.ExpiresAtUtc);
        return new IpaMatchQuestionDto
        {
            QuestionToken = token,
            Accent = snapshot.Accent,
            PromptWord = snapshot.PromptWord,
            Options = snapshot.Options,
            ExpiresAtUtc = snapshot.ExpiresAtUtc
        };
    }

    private string BuildToken(string questionId, long userId, DateTime expiresAtUtc)
    {
        var payload = $"{questionId}|{userId}|{expiresAtUtc.ToUniversalTime():O}";
        var protectedText = _tokenProtector.Protect(payload);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedText));
    }

    private TokenPayload ParseToken(string questionToken)
    {
        try
        {
            var protectedBytes = WebEncoders.Base64UrlDecode(questionToken);
            var protectedText = Encoding.UTF8.GetString(protectedBytes);
            var payload = _tokenProtector.Unprotect(protectedText);
            var segments = payload.Split('|', StringSplitOptions.TrimEntries);
            if (segments.Length != 3)
            {
                throw new FormatException("Invalid token payload.");
            }

            if (!long.TryParse(segments[1], NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
                || !DateTime.TryParse(
                    segments[2],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var expiresAtUtc))
            {
                throw new FormatException("Invalid token payload.");
            }

            return new TokenPayload(segments[0], userId, expiresAtUtc);
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException or ArgumentException)
        {
            throw new PronunciationAssessmentUnavailableException(
                "Question token không hợp lệ.",
                StatusCodes.Status400BadRequest,
                "invalid_question_token",
                exception);
        }
    }

    private static List<(string Raw, string Normalized)> ExtractWords(string sentence)
    {
        var results = new List<(string Raw, string Normalized)>();
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return results;
        }

        foreach (var token in sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var raw = token.Trim();
            var normalized = NormalizeWord(raw);
            if (normalized.Length == 0)
            {
                continue;
            }
            results.Add((raw, normalized));
        }

        return results;
    }

    private static bool IsEligiblePromptWord(string raw, string normalized)
    {
        if (normalized.Length < 2 || normalized.Length > 20)
        {
            return false;
        }

        if (StopWords.Contains(normalized))
        {
            return false;
        }

        if (normalized.Any(char.IsDigit))
        {
            return false;
        }

        if (raw.Length <= 4 && raw.All(char.IsUpper))
        {
            return false;
        }

        return true;
    }

    private static string NormalizeWord(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetter(character) || character is '\'' or '-')
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }
        return builder.ToString().Trim('\'', '-');
    }

    private static string NormalizeIpaForDisplay(string value)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (normalized[0] != '/')
        {
            normalized = $"/{normalized}";
        }
        if (normalized[^1] != '/')
        {
            normalized = $"{normalized}/";
        }
        return normalized;
    }

    private static bool IsValidIpa(string? value) => NormalizeIpa(value ?? string.Empty).Length >= 2;

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

    private static string GenerateDistractorIpa(string correctIpa, int salt)
    {
        var normalized = NormalizeIpa(correctIpa);
        if (normalized.Length == 0)
        {
            return "/ə/";
        }

        var chars = normalized.ToCharArray();
        var position = Math.Min(salt % chars.Length, chars.Length - 1);
        var replacement = chars[position] switch
        {
            'ə' => 'ʌ',
            'ɪ' => 'i',
            'i' => 'ɪ',
            'u' => 'ʊ',
            'ʊ' => 'u',
            'æ' => 'e',
            'e' => 'æ',
            'θ' => 's',
            'ð' => 'd',
            'ʃ' => 's',
            't' => 'd',
            'd' => 't',
            _ => 'ə'
        };
        chars[position] = replacement;
        return $"/{new string(chars)}/";
    }

    private static void Shuffle(List<string> values)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static string BuildQuestionCacheKey(string questionId) => $"ipa-question:{questionId}";

    private static string BuildLatestQuestionKey(long userId, long lessonId, long sentenceId, string accent) =>
        $"ipa-question:latest:{userId}:{lessonId}:{sentenceId}:{accent}";

    private static void EnsureGamificationSucceeded(GamificationTransactionDto transaction)
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

    private sealed class IpaQuestionSnapshot
    {
        public string QuestionId { get; init; } = string.Empty;
        public long UserId { get; init; }
        public long LessonId { get; init; }
        public long SentenceId { get; init; }
        public string Accent { get; init; } = Accents.EnUs;
        public string PromptWord { get; init; } = string.Empty;
        public IReadOnlyList<IpaMatchOptionDto> Options { get; init; } = [];
        public string CorrectOptionId { get; init; } = string.Empty;
        public DateTime ExpiresAtUtc { get; init; }
        public string IdempotencyKey { get; init; } = string.Empty;
        public PracticeAnswerEvaluationDto? Evaluation { get; set; }
    }

    private sealed record TokenPayload(
        string QuestionId,
        long UserId,
        DateTime ExpiresAtUtc);
}
