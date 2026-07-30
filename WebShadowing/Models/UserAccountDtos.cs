namespace WebShadowing.Models;

public sealed record UserMeDto(
    long UserId,
    string FullName,
    string Email,
    string LearningMode,
    byte PronunciationTarget,
    string Accent,
    bool IsVip,
    bool OnboardingCompleted,
    string VipEntitlementSource);

public sealed record CompleteOnboardingResponseDto(
    UserMeDto User,
    string RedirectUrl);

public sealed class VipStubOptions
{
    public const string SectionName = "VipStub";

    public bool Enabled { get; set; }
}
