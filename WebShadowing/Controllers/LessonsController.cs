using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/lessons")]
public sealed class LessonsController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IHostEnvironment _env;

    public LessonsController(ICourseService courseService, IHostEnvironment env)
    {
        _courseService = courseService;
        _env = env;
    }

    [HttpGet("{lessonId:long}")]
    public async Task<IActionResult> GetLesson(
        long lessonId,
        [FromQuery] string? mode,
        CancellationToken cancellationToken)
    {
        var effectiveMode = (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode))
            ? mode
            : LearningModes.Casual;

        var result = await _courseService.GetLessonAsync(
            lessonId,
            NormalizeMode(effectiveMode),
            PronunciationTargets.Comprehension70,
            cancellationToken);

        return result.Status switch
        {
            LessonLookupStatus.Found => Ok(result.Lesson),
            LessonLookupStatus.Forbidden => Forbid(),
            _ => NotFound()
        };
    }

    private static string NormalizeMode(string mode)
    {
        return mode.Trim().ToLowerInvariant();
    }
}
