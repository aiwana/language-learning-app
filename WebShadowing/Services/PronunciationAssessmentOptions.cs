namespace WebShadowing.Services;

public sealed class PronunciationAssessmentOptions
{
    public const string SectionName = "PronunciationAssessment";

    public bool EnableOpenAiFallback { get; set; }
    public int MaxAudioDurationSeconds { get; set; } = 30;
    public int ProviderTimeoutSeconds { get; set; } = 20;
    public string AzureProviderName { get; set; } = "azure-speech";
    public string OpenAiProviderName { get; set; } = "openai-fallback";
    public Dictionary<string, PronunciationModeProfileOptions> ModeProfiles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["casual"] = new()
            {
                AccuracyWeight = 0.35m,
                FluencyWeight = 0.40m,
                CompletenessWeight = 0.15m,
                ProsodyWeight = 0.10m
            },
            ["academic"] = new()
            {
                AccuracyWeight = 0.40m,
                FluencyWeight = 0.20m,
                CompletenessWeight = 0.25m,
                ProsodyWeight = 0.15m
            },
            ["professional"] = new()
            {
                AccuracyWeight = 0.30m,
                FluencyWeight = 0.25m,
                CompletenessWeight = 0.20m,
                ProsodyWeight = 0.25m
            }
        };
}

public sealed class PronunciationModeProfileOptions
{
    public decimal AccuracyWeight { get; set; }
    public decimal FluencyWeight { get; set; }
    public decimal CompletenessWeight { get; set; }
    public decimal ProsodyWeight { get; set; }
}
