using Microsoft.Extensions.Options;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class PronunciationScoreProfileServiceTests
{
    [Fact]
    public void ComputeOverallScore_UsesModeWeights_WhenComponentScoresExist()
    {
        var options = Options.Create(new PronunciationAssessmentOptions());
        var service = new PronunciationScoreProfileService(options);
        var result = new PronunciationAssessmentResult
        {
            OverallScore = 10,
            AccuracyScore = 80,
            FluencyScore = 60,
            CompletenessScore = 90,
            ProsodyScore = 70
        };

        var score = service.ComputeOverallScore(LearningModes.Academic, result);

        Assert.Equal(77, score);
    }

    [Fact]
    public void ComputeOverallScore_FallsBackToProviderOverall_WhenNoComponents()
    {
        var options = Options.Create(new PronunciationAssessmentOptions());
        var service = new PronunciationScoreProfileService(options);
        var result = new PronunciationAssessmentResult { OverallScore = 67 };

        var score = service.ComputeOverallScore(LearningModes.Professional, result);

        Assert.Equal(67, score);
    }
}
