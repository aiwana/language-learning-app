namespace WebShadowing.Models;

public class CourseLibraryViewModel
{
    public string LearningMode { get; set; } = LearningModes.Casual;
    public string LearningModeLabel { get; set; } = "Giao tiếp";
    public string ModeIcon { get; set; } = "compass";

    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }

    public IReadOnlyList<CourseLibraryDto> CurriculumCourses { get; set; } = [];
    public IReadOnlyList<CourseLibraryDto> VideoBankCourses { get; set; } = [];

    public bool HasCurriculum => CurriculumCourses.Count > 0;
    public bool HasVideoBank => VideoBankCourses.Count > 0;
    public bool HasAnyContent => HasCurriculum || HasVideoBank;
}
