namespace WebShadowing.Models;

public sealed class LibraryResponseDto
{
    public string LearningMode { get; set; } = LearningModes.Casual;
    public LibraryCourseSectionDto Curriculum { get; set; } = new();
    public LibraryCourseSectionDto VideoBank { get; set; } = new();
    public AiLessonsSectionDto AiLessons { get; set; } = new();
}

public sealed class LibraryCourseSectionDto
{
    public IReadOnlyList<CourseLibraryDto> Courses { get; set; } = [];
}

public sealed class AiLessonsSectionDto
{
    public IReadOnlyList<object> Items { get; set; } = [];
    public bool ComingSoon { get; set; } = true;
}

public sealed class CourseLibraryDto
{
    public long CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Level { get; set; } = string.Empty;
    public string LearningMode { get; set; } = LearningModes.Casual;
    public string CourseType { get; set; } = CourseTypes.Curriculum;
    public IReadOnlyList<LessonSummaryDto> Lessons { get; set; } = [];
}

public sealed class CoursesListResponseDto
{
    public IReadOnlyList<CourseSummaryDto> Items { get; set; } = [];
}

public sealed class CourseSummaryDto
{
    public long CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Level { get; set; } = string.Empty;
    public string CourseType { get; set; } = CourseTypes.Curriculum;
    public string LearningMode { get; set; } = LearningModes.Casual;
    public int LessonCount { get; set; }
    public int LessonsWithContentCount { get; set; }
}

public sealed class CourseDetailDto
{
    public long CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Level { get; set; } = string.Empty;
    public string CourseType { get; set; } = CourseTypes.Curriculum;
    public string LearningMode { get; set; } = LearningModes.Casual;
    public IReadOnlyList<LessonSummaryDto> Lessons { get; set; } = [];
}

public sealed class LessonSummaryDto
{
    public long LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int LessonOrder { get; set; }
    public int Duration { get; set; }
    public bool HasContent { get; set; }
    public string Source { get; set; } = LessonSources.Curated;
    public IReadOnlyList<string> MaterialTypes { get; set; } = [];
}

public sealed class LessonDetailDto
{
    public long LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int LessonOrder { get; set; }
    public int Duration { get; set; }
    public string Source { get; set; } = LessonSources.Curated;
    public LessonCourseDto Course { get; set; } = new();
    public IReadOnlyList<LessonMaterialDto> Materials { get; set; } = [];
    public LessonMediaDto Media { get; set; } = new();
    public IReadOnlyList<LessonSentenceDto> Sentences { get; set; } = [];
    public byte PronunciationTarget { get; set; } = PronunciationTargets.Comprehension70;
}

public sealed class LessonCourseDto
{
    public long CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseType { get; set; } = CourseTypes.Curriculum;
    public string LearningMode { get; set; } = LearningModes.Casual;
    public string Level { get; set; } = string.Empty;
}

public sealed class LessonMaterialDto
{
    public string MaterialType { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
}

public sealed class LessonMediaDto
{
    public string? YoutubeId { get; set; }
    public string? AudioUrl { get; set; }
    public string? VideoUrl { get; set; }
}

public sealed class LessonSentenceDto
{
    public long SentenceId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Translation { get; set; }
}
