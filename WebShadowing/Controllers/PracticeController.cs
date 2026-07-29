using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/practice")]
public sealed class PracticeController : ControllerBase
{
    private const long MaxAudioBytes = 10 * 1024 * 1024;
    private readonly IPracticeEvaluationService _practiceEvaluationService;

    public PracticeController(
        IPracticeEvaluationService practiceEvaluationService)
    {
        _practiceEvaluationService = practiceEvaluationService;
    }

    [HttpPost("evaluate-shadowing")]
    [EnableRateLimiting("pronunciation-ai")]
    [RequestSizeLimit(MaxAudioBytes + 1024 * 64)]
    public async Task<IActionResult> EvaluateShadowing(
        [FromForm] long lessonId,
        [FromForm] long sentenceId,
        [FromForm] int sentenceIndex,
        [FromForm] IFormFile? audio,
        CancellationToken cancellationToken)
    {
        if (audio is null || audio.Length == 0)
        {
            return BadRequest(new ApiErrorDto
            {
                ErrorCode = "empty_audio",
                Message = "Không nhận được file thu âm."
            });
        }
        if (audio.Length > MaxAudioBytes)
        {
            return BadRequest(new ApiErrorDto
            {
                ErrorCode = "audio_too_large",
                Message = "File thu âm vượt quá giới hạn 10 MB."
            });
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ApiErrorDto
            {
                ErrorCode = "invalid_idempotency_key",
                Message = "Thiếu Idempotency-Key trong request header."
            });
        }

        await using var stream = new MemoryStream();
        await audio.CopyToAsync(stream, cancellationToken);
        var audioBytes = stream.ToArray();

        try
        {
            var result = await _practiceEvaluationService.EvaluateAsync(
                new EvaluateShadowingCommand(
                    lessonId,
                    sentenceId,
                    sentenceIndex,
                    audioBytes,
                    GetAudioFormat(audio),
                    audio.ContentType ?? string.Empty,
                    idempotencyKey),
                cancellationToken);

            return Ok(result);
        }
        catch (PronunciationAssessmentUnavailableException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorDto
            {
                ErrorCode = exception.ErrorCode,
                Message = exception.Message
            });
        }
    }

    [HttpPost("evaluate-answer")]
    public async Task<IActionResult> EvaluateAnswer(
        [FromBody] PracticeAnswerRequestDto request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ApiErrorDto
            {
                ErrorCode = "invalid_idempotency_key",
                Message = "Thiếu Idempotency-Key trong request header."
            });
        }

        try
        {
            var result = await _practiceEvaluationService.EvaluateAnswerAsync(
                new EvaluatePracticeAnswerCommand(
                    request.LessonId,
                    request.SentenceId,
                    request.PracticeTab,
                    request.Answer,
                    idempotencyKey),
                cancellationToken);
            return Ok(result);
        }
        catch (PronunciationAssessmentUnavailableException exception)
        {
            return StatusCode(exception.StatusCode, new ApiErrorDto
            {
                ErrorCode = exception.ErrorCode,
                Message = exception.Message
            });
        }
    }

    private static string GetAudioFormat(IFormFile audio)
    {
        var extension = Path.GetExtension(audio.FileName).TrimStart('.').ToLowerInvariant();
        return extension is "mp3" or "wav" ? extension : "wav";
    }
}
