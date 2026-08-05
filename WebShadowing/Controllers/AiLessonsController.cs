using Microsoft.AspNetCore.Authorization;
// Chức năng: API tạo bài AI, liệt kê draft/saved, lưu và xóa bài.
// Phụ trách chính: Minh Anh. Phối hợp Minh khi thay đổi schema/ownership.
// Draft có thời hạn 24 giờ theo AiLesson:PreviewLifetimeMinutes = 1440.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController, Authorize, Route("api/ai-lessons")]
public sealed class AiLessonsController : ControllerBase
{
    private readonly IAiLessonGenerationService _service;
    private readonly IUserContextService _userContext;
    public AiLessonsController(IAiLessonGenerationService service, IUserContextService userContext) { _service = service; _userContext = userContext; }

    [HttpPost("generate"), EnableRateLimiting("ai-generation")]
    public async Task<IActionResult> Generate(GenerateAiLessonRequestDto request, CancellationToken cancellationToken)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();
        try { return Ok(await _service.GenerateAsync(id, request, cancellationToken)); }
        catch (OpenAiServiceUnavailableException exception) { return StatusCode(503, new { message = exception.Message }); }
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(SaveAiLessonRequestDto request, CancellationToken cancellationToken)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();
        var lesson = await _service.SaveAsync(id, request.PreviewId, cancellationToken);
        return lesson is null ? NotFound(new { message = "Bản xem trước đã hết hạn hoặc không tồn tại." }) : Ok(lesson);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => _userContext.GetCurrentUserId() is long id
        ? Ok(await _service.ListAsync(id, cancellationToken)) : Unauthorized();

    [HttpGet("previews")]
    public async Task<IActionResult> ListPreviews(CancellationToken cancellationToken) =>
        _userContext.GetCurrentUserId() is long id
            ? Ok(await _service.ListPreviewsAsync(id, cancellationToken))
            : Unauthorized();

    [HttpDelete("preview/{id:guid}")]
    public async Task<IActionResult> DeletePreview(Guid id, CancellationToken cancellationToken) =>
        _userContext.GetCurrentUserId() is long userId && await _service.DeletePreviewAsync(userId, id, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) =>
        _userContext.GetCurrentUserId() is long userId && await _service.DeleteAsync(userId, id, cancellationToken) ? NoContent() : NotFound();
}
