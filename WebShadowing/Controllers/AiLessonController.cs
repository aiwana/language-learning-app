using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Infrastructure;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiLessonController : ControllerBase
{
    private const string DraftSessionKey = "AiLessonDraft";

    private readonly IAiLessonService _aiLessonService;
    private readonly IUserContextService _userContext;

    public AiLessonController(IAiLessonService aiLessonService, IUserContextService userContext)
    {
        _aiLessonService = aiLessonService;
        _userContext = userContext;
    }

    [HttpPost("generate")]
    public IActionResult Generate([FromBody] GenerateAiLessonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { message = "Prompt không được để trống." });
        }

        var draft = _aiLessonService.GenerateFromPrompt(request.Prompt, request.Level);
        HttpContext.Session.SetJson(DraftSessionKey, draft);

        return Ok(new
        {
            previewUrl = Url.Action("LessonPreview", "Home")
        });
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var draft = HttpContext.Session.GetJson<GeneratedLessonDto>(DraftSessionKey);
        if (draft is null)
        {
            return BadRequest(new { message = "Không tìm thấy bài AI trong phiên làm việc. Hãy tạo lại bài học." });
        }

        var result = await _aiLessonService.SaveDraftAsync(userId.Value, draft, cancellationToken);
        HttpContext.Session.Remove(DraftSessionKey);

        return Ok(new
        {
            lessonId = result.LessonId,
            detailUrl = Url.Action("LessonDetail", "Home", new { id = result.LessonId })
        });
    }
}
