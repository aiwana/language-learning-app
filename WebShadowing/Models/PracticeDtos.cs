namespace WebShadowing.Models;

public sealed class ShadowingEvaluationDto
{
    public int Score { get; set; }
    public bool Passed { get; set; }
    public byte PronunciationTarget { get; set; }
    public string Provider { get; set; } = "unknown";
    public string Accent { get; set; } = Accents.EnUs;
    public string LearningMode { get; set; } = LearningModes.Casual;
    public int? AccuracyScore { get; set; }
    public int? FluencyScore { get; set; }
    public int? CompletenessScore { get; set; }
    public int? ProsodyScore { get; set; }
    public string Transcript { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public IReadOnlyList<WordFeedbackDto> Words { get; set; } = [];
}

public sealed class WordFeedbackDto
{
    public string Word { get; set; } = string.Empty;
    public string AccuracyCode { get; set; } = "warning";
    public string? Correction { get; set; }
    public IReadOnlyList<PhonemeFeedbackDto> Phonemes { get; set; } = [];
}

public sealed class PhonemeFeedbackDto
{
    public string Symbol { get; set; } = string.Empty;
    public string AccuracyCode { get; set; } = "warning";
}

public sealed class ApiErrorDto
{
    public string ErrorCode { get; set; } = "unknown_error";
    public string Message { get; set; } = string.Empty;
}

public sealed class WordMeaningRequestDto
{
    public string Word { get; set; } = string.Empty;
    public string? Context { get; set; }
}

public sealed class WordMeaningDto
{
    public string Word { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Provider { get; set; } = "fallback";
}

public sealed class WordIpaBatchRequestDto
{
    public IReadOnlyList<string> Words { get; set; } = [];
}

public sealed class WordIpaDto
{
    public string Word { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
}
