using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api")]
public sealed class LanguageReferenceController : ControllerBase
{
    private const int MaxWordLength = 80;
    private const int MaxContextLength = 500;
    private readonly ILanguageReferenceService _languageReferenceService;

    public LanguageReferenceController(ILanguageReferenceService languageReferenceService)
    {
        _languageReferenceService = languageReferenceService;
    }

    [HttpPost("word-meaning")]
    [EnableRateLimiting("language-reference-ai")]
    public async Task<IActionResult> GetWordMeaning(
        [FromBody] WordMeaningRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Word) || request.Word.Length > MaxWordLength)
        {
            return BadRequest(new { message = "Từ cần tra không hợp lệ." });
        }
        if (request.Context?.Length > MaxContextLength)
        {
            return BadRequest(new { message = $"Ngữ cảnh không được vượt quá {MaxContextLength} ký tự." });
        }

        return Ok(await _languageReferenceService.GetMeaningAsync(
            request.Word,
            request.Context,
            cancellationToken));
    }

    [HttpPost("word-ipa/batch")]
    [EnableRateLimiting("language-reference-ai")]
    public async Task<IActionResult> GetWordIpaBatch(
        [FromBody] WordIpaBatchRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Words is not { Count: > 0 and <= 40 }
            || request.Words.Any(word => string.IsNullOrWhiteSpace(word) || word.Length > MaxWordLength))
        {
            return BadRequest(new { message = "Danh sách phải có từ 1 đến 40 từ hợp lệ, mỗi từ tối đa 80 ký tự." });
        }

        return Ok(await _languageReferenceService.GetIpaBatchAsync(request.Words, cancellationToken));
    }
}
