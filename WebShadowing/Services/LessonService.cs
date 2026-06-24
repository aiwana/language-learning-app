using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class LessonService : ILessonService
{
    private readonly AppDbContext _db;

    public LessonService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LibraryViewModel> GetLibraryAsync(long? userId, CancellationToken cancellationToken = default)
    {
        var courses = await _db.Courses
            .AsNoTracking()
            .Include(c => c.Lessons.OrderBy(l => l.LessonOrder))
            .Where(c => c.CourseType != CourseTypes.AiSaved)
            .OrderBy(c => c.CourseId)
            .ToListAsync(cancellationToken);

        var myAiLessons = userId is null
            ? new List<Lesson>()
            : await _db.Lessons
                .AsNoTracking()
                .Include(l => l.Course)
                .Where(l => l.Source == LessonSources.Ai && l.CreatedByUserId == userId)
                .OrderByDescending(l => l.LessonId)
                .ToListAsync(cancellationToken);

        return new LibraryViewModel
        {
            VideoBankCourses = courses.Where(c => c.CourseType == CourseTypes.VideoBank).ToList(),
            CurriculumCourses = courses.Where(c => c.CourseType == CourseTypes.Curriculum).ToList(),
            MyAiLessons = myAiLessons,
            IsAuthenticated = userId is not null
        };
    }

    public async Task<Lesson?> GetLessonWithDetailsAsync(long lessonId, CancellationToken cancellationToken = default)
    {
        return await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .Include(l => l.Materials)
            .FirstOrDefaultAsync(l => l.LessonId == lessonId, cancellationToken);
    }
}
