using Microsoft.AspNetCore.Authorization;
// Chức năng: API hồ sơ, learning settings, đổi mode và onboarding cho trang Tài khoản.
// Phụ trách trang/API: Hải Anh. Minh review auth, field-level security và policy đổi mode.
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
    private readonly IUserProfileService _profileService;
    private readonly IModeChangeService _modeChangeService;

    public UserController(IAuthService authService, IUserContextService userContext, IUserProfileService profileService, IModeChangeService modeChangeService)
    {
        _authService = authService;
        _userContext = userContext;
        _profileService = profileService;
        _modeChangeService = modeChangeService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken) =>
        _userContext.GetCurrentUserId() is long id
            ? (await _profileService.GetAsync(id, cancellationToken) is { } profile ? Ok(profile) : NotFound())
            : Unauthorized();

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequestDto request, CancellationToken cancellationToken) =>
        _userContext.GetCurrentUserId() is long id
            ? (await _profileService.UpdateProfileAsync(id, request, cancellationToken) is { } profile ? Ok(profile) : NotFound())
            : Unauthorized();

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(UpdateLearningSettingsRequestDto request, CancellationToken cancellationToken)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();
        try
        {
            var profile = await _profileService.UpdateLearningSettingsAsync(id, request, cancellationToken);
            return profile is null ? NotFound() : Ok(profile);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("mode")]
    public async Task<IActionResult> ChangeMode(ChangeLearningModeRequestDto request, CancellationToken cancellationToken)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();
        var result = await _modeChangeService.ChangeAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result) : Conflict(result);
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

        var redirect = model.Plan == "vip" && !user.IsVip
            ? Url.Action("Settings", "Home", new { checkout = "vip" }) ?? "/Home/Settings?checkout=vip"
            : Url.Action("Index", "Home") ?? "/";
        return Ok(new CompleteOnboardingResponseDto(user, redirect));
    }
}
