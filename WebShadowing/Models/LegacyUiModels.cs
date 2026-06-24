namespace WebShadowing.Models;

public enum UserLevel
{
    Casual,
    Academic,
    Professional
}

public enum LearningGoal
{
    Fluency50,
    Comprehension70,
    Accent90
}

public enum TargetAccent
{
    US,
    UK
}

public class UserProfile
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserLevel Level { get; set; }
    public LearningGoal Goal { get; set; }
    public TargetAccent TargetAccent { get; set; }
    public bool IsPremium { get; set; }
}

public class UserStats
{
    public int Streak { get; set; }
    public int TotalSentences { get; set; }
    public int Hearts { get; set; }
    public int Exp { get; set; }
}

public static class StaticData
{
    public static UserProfile DefaultProfile { get; } = new();
    public static UserStats DefaultStats { get; } = new();
}

public class Flashcard
{
    public string Word { get; set; } = string.Empty;
    public int Box { get; set; }
    public string Ipa { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string SentenceContext { get; set; } = string.Empty;
}

public class FavoriteSentenceLine
{
    public string Text { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
}

public class FavoriteSentence
{
    public string LessonTitle { get; set; } = string.Empty;
    public FavoriteSentenceLine Sentence { get; set; } = new();
}

public class LessonSentenceViewModel
{
    public string Text { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public double StartTime { get; set; }
    public double EndTime { get; set; }
}

public class LessonPageViewModel
{
    public long LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public bool IsGenerated { get; set; }
    public string? YoutubeId { get; set; }
    public string? AudioUrl { get; set; }
    public List<LessonSentenceViewModel> Sentences { get; set; } = [];
}
