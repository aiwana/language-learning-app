using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IAiLessonService
{
    GeneratedLessonDto GenerateFromPrompt(string prompt, string level = "Beginner");
    Task<SaveAiLessonResult> SaveDraftAsync(long userId, GeneratedLessonDto draft, CancellationToken cancellationToken = default);
}
