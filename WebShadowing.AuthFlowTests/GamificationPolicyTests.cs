using WebShadowing.Services;
// Kịch bản test: reward, heart penalty, VIP và streak.
// Phụ trách test: Hải Anh. Minh xác nhận gamification policy.
using Xunit;

namespace WebShadowing.AuthFlowTests;

public sealed class GamificationPolicyTests
{
    [Theory]
    [InlineData(true, false, 20, 20)]
    [InlineData(true, true, 20, 0)]
    [InlineData(false, false, 20, 0)]
    public void CompletionReward_IsGrantedOnlyForFirstPassingCompletion(
        bool passed,
        bool alreadyRewarded,
        int reward,
        int expected)
    {
        Assert.Equal(expected, GamificationPolicy.CalculateCompletionExp(passed, alreadyRewarded, reward));
    }

    [Theory]
    [InlineData(5, 1, false, -1)]
    [InlineData(0, 1, false, 0)]
    [InlineData(5, 1, true, 0)]
    [InlineData(1, 5, false, -1)]
    public void HeartPenalty_NeverGoesBelowZeroAndVipIsExempt(
        int hearts,
        int cost,
        bool isVip,
        int expectedDelta)
    {
        Assert.Equal(expectedDelta, GamificationPolicy.CalculateHeartPenalty(hearts, cost, isVip));
        Assert.True(hearts + expectedDelta >= 0);
    }

    [Fact]
    public void Streak_OnlyAdvancesOncePerBusinessDateAndResetsAfterGap()
    {
        var today = new DateOnly(2026, 7, 20);

        Assert.Equal(4, GamificationPolicy.CalculateStreak(today, today, 4));
        Assert.Equal(5, GamificationPolicy.CalculateStreak(today.AddDays(-1), today, 4));
        Assert.Equal(1, GamificationPolicy.CalculateStreak(today.AddDays(-2), today, 4));
        Assert.Equal(1, GamificationPolicy.CalculateStreak(null, today, 0));
    }
}
