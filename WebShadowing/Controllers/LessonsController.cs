using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/lessons")]
public sealed class LessonsController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly IHostEnvironment _env;

    public LessonsController(
        ICourseService courseService,
        IUserContextService userContext,
        IHostEnvironment env)
    {
        _courseService = courseService;
        _userContext = userContext;
        _env = env;
    }

    [HttpGet("{lessonId:long}")]
    public async Task<IActionResult> GetLesson(
        long lessonId,
        [FromQuery] string? mode,
        CancellationToken cancellationToken)
    {
        var userMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var effectiveMode = _env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode) ? mode : userMode;
        var pronunciationTarget = await _userContext.GetPronunciationTargetAsync(cancellationToken);

        var result = await _courseService.GetLessonAsync(
            lessonId,
            NormalizeMode(effectiveMode),
            pronunciationTarget,
            cancellationToken);

        return result.Status switch
        {
            LessonLookupStatus.Found => Ok(result.Lesson),
            LessonLookupStatus.Forbidden => Forbid(),
            _ => NotFound()
        };
    }

    private static string NormalizeMode(string mode) =>
        mode.Trim().ToLowerInvariant();
}
