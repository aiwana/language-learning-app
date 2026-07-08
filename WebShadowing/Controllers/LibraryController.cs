using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/library")]
public sealed class LibraryController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly IHostEnvironment _env;

    public LibraryController(
        ICourseService courseService,
        IUserContextService userContext,
        IHostEnvironment env)
    {
        _courseService = courseService;
        _userContext = userContext;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetLibrary([FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var userMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var effectiveMode = (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode))
            ? mode
            : userMode;

        var response = await _courseService.GetLibraryAsync(NormalizeMode(effectiveMode), cancellationToken);
        return Ok(response);
    }

    private static string NormalizeMode(string mode)
    {
        return mode.Trim().ToLowerInvariant();
    }
}
