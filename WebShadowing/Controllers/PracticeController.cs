using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PracticeController : ControllerBase
{
    private readonly IPracticeService _practiceService;

    public PracticeController(IPracticeService practiceService)
    {
        _practiceService = practiceService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest req, CancellationToken cancellationToken)
    {
        var userIdVal = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdVal, out var userId))
        {
            return Unauthorized();
        }

        var res = await _practiceService.StartSessionAsync(userId, req.LessonId, cancellationToken);
        if (!res.Succeeded)
        {
            return BadRequest(new { message = res.ErrorMessage });
        }

        return Ok(new { sessionId = res.SessionId });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteSession([FromBody] CompleteSessionRequest req, CancellationToken cancellationToken)
    {
        var userIdVal = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdVal, out var userId))
        {
            return Unauthorized();
        }

        var res = await _practiceService.CompleteSessionAsync(userId, req.SessionId, req.OverallScore, cancellationToken);
        if (!res.Succeeded)
        {
            return BadRequest(new { message = res.ErrorMessage });
        }

        return Ok(new { message = "Lưu kết quả thành công" });
    }
}

public class StartSessionRequest
{
    public long LessonId { get; set; }
}

public class CompleteSessionRequest
{
    public long SessionId { get; set; }
    public decimal OverallScore { get; set; }
}
