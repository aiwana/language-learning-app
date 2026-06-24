namespace WebShadowing.Models;

public class GeneratedLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner";
    public string? Prompt { get; set; }
    public List<LessonSentenceViewModel> Sentences { get; set; } = [];
}

public class GenerateAiLessonRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner";
}

public class SaveAiLessonResult
{
    public long LessonId { get; set; }
}
