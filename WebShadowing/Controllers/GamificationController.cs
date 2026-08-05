using Microsoft.AspNetCore.Authorization;
// Chức năng: đọc balance và đổi EXP lấy tim cho navbar/trang Tiến trình.
// Phụ trách hiển thị và test: Hải Anh. Phụ trách rule/persistence: Minh.
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/gamification")]
public sealed class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamificationService;
    private readonly IUserContextService _userContext;

    public GamificationController(
        IGamificationService gamificationService,
        IUserContextService userContext)
    {
        _gamificationService = gamificationService;
        _userContext = userContext;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var balance = await _gamificationService.GetBalanceAsync(userId.Value, cancellationToken);
        return balance is null ? NotFound() : Ok(balance);
    }

    [HttpPost("exchange-heart")]
    public async Task<IActionResult> ExchangeHeart(
        [FromBody] HeartExchangeRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _gamificationService.ExchangeHeartAsync(
            userId.Value,
            request.IdempotencyKey.Trim(),
            cancellationToken);
        return result.Succeeded ? Ok(result) : Conflict(result);
    }
}
