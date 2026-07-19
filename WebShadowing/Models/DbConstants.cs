namespace WebShadowing.Models;

public static class CourseLevels
{
    public const string Beginner = "Beginner";
    public const string Intermediate = "Intermediate";
    public const string Advanced = "Advanced";
}

public static class MaterialTypes
{
    public const string Audio = "audio";
    public const string Video = "video";
    public const string Transcript = "transcript";
    public const string Text = "text";
}

public static class CourseTypes
{
    public const string VideoBank = "video_bank";
    public const string Curriculum = "curriculum";
    public const string AiSaved = "ai_saved";
}

public static class LessonSources
{
    public const string Curated = "curated";
    public const string Ai = "ai";
}

public static class LearningModes
{
    public const string Casual = "casual";
    public const string Academic = "academic";
    public const string Professional = "professional";
}

public static class PronunciationTargets
{
    public const byte Fluency50 = 50;
    public const byte Comprehension70 = 70;
    public const byte Accent90 = 90;
}

public static class Accents
{
    public const string EnUs = "en-us";
    public const string EnGb = "en-gb";
}

public static class PracticeTabs
{
    public const string Shadowing = "shadowing";
    public const string AiDialogue = "ai-dialogue";
    public const string Dictation = "dictation";
    public const string IpaMatch = "ipa-match";
}

public static class ProgressStatuses
{
    public const string NotStarted = "not_started";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
}

public static class ExerciseTypes
{
    public const string Pronunciation = "pronunciation";
    public const string Shadowing = "shadowing";
    public const string Dictation = "dictation";
    public const string IpaMatch = "ipa_match";
    public const string AiDialogue = "ai_dialogue";
}

public static class AttemptResults
{
    public const string Pending = "pending";
    public const string Passed = "passed";
    public const string Failed = "failed";
    public const string Abandoned = "abandoned";
}

public static class SourceReviewStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

public static class ThemePreferences
{
    public const string System = "system";
    public const string Light = "light";
    public const string Dark = "dark";
}

public static class ModeChangeActors
{
    public const string User = "user";
    public const string Admin = "admin";
    public const string System = "system";
    public const string Onboarding = "onboarding";
}

public static class SubscriptionStatuses
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string PastDue = "past_due";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";
}

public static class BillingPeriods
{
    public const string Monthly = "monthly";
    public const string Yearly = "yearly";
    public const string Lifetime = "lifetime";
}

public static class PaymentStatuses
{
    public const string Pending = "pending";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Refunded = "refunded";
}

public static class PaymentTypes
{
    public const string Purchase = "purchase";
    public const string Renewal = "renewal";
    public const string Refund = "refund";
}
