using Microsoft.AspNetCore.Authorization;
// Chức năng: API phiên Đối thoại AI VIP, nhận text hoặc audio và trả transcript/reply/audio.
// Phụ trách chính: Minh Anh. Phối hợp Minh về auth, VIP entitlement và quyền riêng tư audio.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController, Authorize, Route("api/ai-dialogue")]
public sealed class AiDialogueController : ControllerBase
{
    private const long MaxAudioBytes = 10 * 1024 * 1024;
    private readonly IAiDialogueService _service;
    private readonly IUserContextService _userContext;
    public AiDialogueController(IAiDialogueService service, IUserContextService userContext) { _service = service; _userContext = userContext; }

    [HttpPost("sessions"), EnableRateLimiting("ai-dialogue")]
    public Task<IActionResult> Start(StartDialogueRequestDto request, CancellationToken cancellationToken) => Execute(async id => Ok(await _service.StartAsync(id, request.LessonId, cancellationToken)));

    [HttpGet("sessions/{id:long}")]
    public Task<IActionResult> Get(long id, CancellationToken cancellationToken) => Execute(async userId =>
        await _service.GetAsync(userId, id, cancellationToken) is { } session ? Ok(session) : NotFound());

    [HttpPost("sessions/{id:long}/messages"), EnableRateLimiting("ai-dialogue")]
    public Task<IActionResult> Send(long id, DialogueMessageRequestDto request, CancellationToken cancellationToken) => Execute(async userId =>
        await _service.SendTextAsync(userId, id, request.Message, cancellationToken) is { } reply ? Ok(reply) : NotFound());

    [HttpPost("sessions/{id:long}/audio"), EnableRateLimiting("ai-dialogue"), RequestSizeLimit(MaxAudioBytes + 65536)]
    public Task<IActionResult> SendAudio(long id, IFormFile? audio, CancellationToken cancellationToken) => Execute(async userId =>
    {
        if (audio is null || audio.Length == 0) return BadRequest(new { message = "Không nhận được bản thu." });
        if (audio.Length > MaxAudioBytes) return BadRequest(new { message = "Bản thu vượt quá 10 MB." });
        await using var memory = new MemoryStream();
        await audio.CopyToAsync(memory, cancellationToken);
        var reply = await _service.SendAudioAsync(userId, id, memory.ToArray(), audio.FileName, audio.ContentType, cancellationToken);
        return reply is null ? NotFound() : Ok(reply);
    });

    private async Task<IActionResult> Execute(Func<long, Task<IActionResult>> action)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();
        try { return await action(id); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (OpenAiServiceUnavailableException exception) { return StatusCode(503, new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }
}
