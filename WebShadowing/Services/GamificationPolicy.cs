namespace WebShadowing.Services;

public static class GamificationPolicy
{
    public static int CalculateCompletionExp(bool passed, bool alreadyRewarded, int configuredReward) =>
        passed && !alreadyRewarded ? Math.Max(configuredReward, 0) : 0;

    public static int CalculateHeartPenalty(int hearts, int configuredCost, bool isVip) =>
        isVip ? 0 : -Math.Min(Math.Max(hearts, 0), Math.Max(configuredCost, 0));

    public static int CalculateStreak(DateOnly? lastPracticeDate, DateOnly currentDate, int currentStreak)
    {
        if (lastPracticeDate is null)
        {
            return 1;
        }

        if (lastPracticeDate.Value >= currentDate)
        {
            return Math.Max(currentStreak, 1);
        }

        return lastPracticeDate.Value == currentDate.AddDays(-1)
            ? Math.Max(currentStreak, 0) + 1
            : 1;
    }
}
