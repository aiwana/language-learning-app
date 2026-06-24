namespace WebShadowing.Models;

public class LibraryViewModel
{
    public IReadOnlyList<Course> VideoBankCourses { get; set; } = [];
    public IReadOnlyList<Course> CurriculumCourses { get; set; } = [];
    public IReadOnlyList<Lesson> MyAiLessons { get; set; } = [];
    public bool IsAuthenticated { get; set; }
}
