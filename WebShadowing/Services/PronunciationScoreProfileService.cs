using Microsoft.Extensions.Options;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class PronunciationScoreProfileService
{
    private readonly PronunciationAssessmentOptions _options;

    public PronunciationScoreProfileService(IOptions<PronunciationAssessmentOptions> options)
    {
        _options = options.Value;
    }

    public int ComputeOverallScore(string learningMode, PronunciationAssessmentResult providerResult)
    {
        var profile = ResolveProfile(learningMode);
        var weightedTotal = 0m;
        var weightSum = 0m;

        AddComponent(profile.AccuracyWeight, providerResult.AccuracyScore);
        AddComponent(profile.FluencyWeight, providerResult.FluencyScore);
        AddComponent(profile.CompletenessWeight, providerResult.CompletenessScore);
        AddComponent(profile.ProsodyWeight, providerResult.ProsodyScore);

        if (weightSum <= 0)
        {
            return Math.Clamp(providerResult.OverallScore, 0, 100);
        }

        var computed = weightedTotal / weightSum;
        return (int)Math.Round(Math.Clamp(computed, 0m, 100m), MidpointRounding.AwayFromZero);

        void AddComponent(decimal weight, int? score)
        {
            if (weight <= 0 || score is null)
            {
                return;
            }

            weightedTotal += Math.Clamp(score.Value, 0, 100) * weight;
            weightSum += weight;
        }
    }

    private PronunciationModeProfileOptions ResolveProfile(string learningMode)
    {
        if (_options.ModeProfiles.TryGetValue(learningMode, out var profile))
        {
            return profile;
        }

        if (_options.ModeProfiles.TryGetValue(LearningModes.Casual, out var fallback))
        {
            return fallback;
        }

        return new PronunciationModeProfileOptions
        {
            AccuracyWeight = 0.4m,
            FluencyWeight = 0.3m,
            CompletenessWeight = 0.2m,
            ProsodyWeight = 0.1m
        };
    }
}
