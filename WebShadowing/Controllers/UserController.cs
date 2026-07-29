using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserContextService _userContext;

    public UserController(IAuthService authService, IUserContextService userContext)
    {
        _authService = authService;
        _userContext = userContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserMeDto>> GetMe(CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserAsync(userId.Value, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("onboarding")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CompleteOnboardingResponseDto>> CompleteOnboarding(
        CompleteOnboardingViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _authService.CompleteOnboardingAsync(userId.Value, model, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Message });
        }

        var user = await _authService.GetUserAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new CompleteOnboardingResponseDto(user, Url.Action("Index", "Home") ?? "/"));
    }
}
