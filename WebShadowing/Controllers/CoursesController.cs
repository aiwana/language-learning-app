using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/courses")]
public sealed class CoursesController : ControllerBase
{
    private static readonly HashSet<string> AllowedCourseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        CourseTypes.Curriculum,
        CourseTypes.VideoBank
    };

    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly IHostEnvironment _env;

    public CoursesController(
        ICourseService courseService,
        IUserContextService userContext,
        IHostEnvironment env)
    {
        _courseService = courseService;
        _userContext = userContext;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string type,
        [FromQuery] string? mode,
        CancellationToken cancellationToken)
    {
        var courseType = NormalizeCourseType(type);
        if (courseType is null || !AllowedCourseTypes.Contains(courseType))
        {
            return BadRequest(new { message = "type must be curriculum or video_bank." });
        }

        var userMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var effectiveMode = (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode))
            ? mode
            : userMode;

        var response = await _courseService.GetCoursesAsync(courseType, NormalizeMode(effectiveMode), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{courseId:long}")]
    public async Task<IActionResult> GetCourse(
        long courseId,
        [FromQuery] string? mode,
        CancellationToken cancellationToken)
    {
        var userMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var effectiveMode = (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode))
            ? mode
            : userMode;

        var response = await _courseService.GetCourseAsync(courseId, NormalizeMode(effectiveMode), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    private static string? NormalizeCourseType(string? type)
    {
        return string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToLowerInvariant();
    }

    private static string NormalizeMode(string mode)
    {
        return mode.Trim().ToLowerInvariant();
    }
}
