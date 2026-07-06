using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/library")]
public sealed class LibraryController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IHostEnvironment _env;

    public LibraryController(ICourseService courseService, IHostEnvironment env)
    {
        _courseService = courseService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetLibrary([FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var effectiveMode = (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode))
            ? mode
            : LearningModes.Casual;

        var response = await _courseService.GetLibraryAsync(NormalizeMode(effectiveMode), cancellationToken);
        return Ok(response);
    }

    private static string NormalizeMode(string mode)
    {
        return mode.Trim().ToLowerInvariant();
    }
}
