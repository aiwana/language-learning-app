using WebShadowing.Models;

namespace WebShadowing.Services;

public interface ILessonService
{
    Task<LibraryViewModel> GetLibraryAsync(long? userId, CancellationToken cancellationToken = default);
    Task<Lesson?> GetLessonWithDetailsAsync(long lessonId, CancellationToken cancellationToken = default);
}
