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
