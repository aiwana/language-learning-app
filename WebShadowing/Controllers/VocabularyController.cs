using Microsoft.AspNetCore.Authorization;
// Chức năng: API sổ từ, trạng thái ôn và chấm phát âm từ trên trang Tiến trình & Thẻ nhớ.
// Phụ trách logic tạo từ sai: Minh. Phụ trách trang/kiểm thử: Hải Anh.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController, Authorize, Route("api/vocabulary")]
public sealed class VocabularyController : ControllerBase
{
    private readonly IVocabularyService _service;
    private readonly IUserContextService _userContext;
    private readonly IPronunciationAssessmentService _assessment;
    public VocabularyController(IVocabularyService service, IUserContextService userContext, IPronunciationAssessmentService assessment) { _service = service; _userContext = userContext; _assessment = assessment; }
    private long? UserId => _userContext.GetCurrentUserId();

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        UserId is long id ? Ok(await _service.GetPageAsync(id, status, page, cancellationToken: cancellationToken)) : Unauthorized();

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken) =>
        UserId is long userId ? (await _service.GetAsync(userId, id, cancellationToken) is { } item ? Ok(item) : NotFound()) : Unauthorized();

    [HttpPost]
    public async Task<IActionResult> Add(AddVocabularyRequestDto request, CancellationToken cancellationToken) =>
        UserId is long id ? Ok(await _service.AddAsync(id, request, cancellationToken)) : Unauthorized();

    [HttpPost("{id:long}/mastered")]
    public async Task<IActionResult> Mastered(long id, CancellationToken cancellationToken) =>
        UserId is long userId && await _service.MarkMasteredAsync(userId, id, cancellationToken) ? Ok(new { success = true }) : NotFound();

    [HttpPost("{id:long}/review")]
    public async Task<IActionResult> Review(long id, CancellationToken cancellationToken) =>
        UserId is long userId && await _service.ResetReviewAsync(userId, id, cancellationToken) ? Ok(new { success = true }) : NotFound();

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) =>
        UserId is long userId && await _service.DeleteAsync(userId, id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:long}/pronunciation"), EnableRateLimiting("pronunciation-ai"), RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Pronunciation(long id, IFormFile? audio, CancellationToken cancellationToken)
    {
        if (UserId is not long userId) return Unauthorized();
        var item = await _service.GetAsync(userId, id, cancellationToken);
        if (item is null) return NotFound();
        if (audio is null || audio.Length == 0 || audio.Length > 5 * 1024 * 1024) return BadRequest(new { message = "Bản thu không hợp lệ." });
        await using var memory = new MemoryStream();
        await audio.CopyToAsync(memory, cancellationToken);
        try
        {
            var result = await _assessment.AssessAsync(new PronunciationAssessmentRequest(
                memory.ToArray(),
                "wav",
                await _userContext.GetAccentAsync(cancellationToken),
                await _userContext.GetLearningModeAsync(cancellationToken),
                item.Word,
                item.Ipa,
                await _userContext.GetPronunciationTargetAsync(cancellationToken)), cancellationToken);
            return Ok(result);
        }
        catch (PronunciationAssessmentUnavailableException exception) { return StatusCode(503, new { message = exception.Message }); }
    }
}
