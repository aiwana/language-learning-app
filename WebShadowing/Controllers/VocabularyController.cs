using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/vocabulary")]
public sealed class VocabularyController : ControllerBase
{
    private readonly IVocabularyNotebookService _vocabularyNotebookService;
    private readonly IUserContextService _userContextService;

    public VocabularyController(
        IVocabularyNotebookService vocabularyNotebookService,
        IUserContextService userContextService)
    {
        _vocabularyNotebookService = vocabularyNotebookService;
        _userContextService = userContextService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVocabulary(
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _userContextService.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _vocabularyNotebookService.GetPageAsync(
            userId.Value,
            page,
            pageSize,
            cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddVocabulary(
        [FromBody] AddVocabularyRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _vocabularyNotebookService.UpsertAsync(userId.Value, request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiErrorDto { ErrorCode = "invalid_vocabulary_request", Message = exception.Message });
        }
    }

    [HttpDelete("{vocabularyItemId:long}")]
    public async Task<IActionResult> DeleteVocabulary(long vocabularyItemId, CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return await _vocabularyNotebookService.DeleteAsync(userId.Value, vocabularyItemId, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}