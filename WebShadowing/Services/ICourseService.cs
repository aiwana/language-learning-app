using WebShadowing.Models;

namespace WebShadowing.Services;

public interface ICourseService
{
    Task<LibraryResponseDto> GetLibraryAsync(string learningMode, CancellationToken cancellationToken = default);
    Task<CoursesListResponseDto> GetCoursesAsync(string courseType, string learningMode, CancellationToken cancellationToken = default);
    Task<CourseDetailDto?> GetCourseAsync(long courseId, string learningMode, CancellationToken cancellationToken = default);
    Task<LessonLookupResult> GetLessonAsync(long lessonId, string learningMode, byte pronunciationTarget, CancellationToken cancellationToken = default);
}

public enum LessonLookupStatus
{
    Found,
    NotFound,
    Forbidden
}

public sealed class LessonLookupResult
{
    public LessonLookupStatus Status { get; init; }
    public LessonDetailDto? Lesson { get; init; }

    public static LessonLookupResult NotFound() => new() { Status = LessonLookupStatus.NotFound };
    public static LessonLookupResult Forbidden() => new() { Status = LessonLookupStatus.Forbidden };
    public static LessonLookupResult Found(LessonDetailDto lesson) => new() { Status = LessonLookupStatus.Found, Lesson = lesson };
}
