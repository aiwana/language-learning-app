using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/practice")]
public sealed class PracticeController : ControllerBase
{
    private const long MaxAudioBytes = 10 * 1024 * 1024;
    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly IPronunciationAssessmentService _assessmentService;
    private readonly IHostEnvironment _environment;

    public PracticeController(
        ICourseService courseService,
        IUserContextService userContext,
        IPronunciationAssessmentService assessmentService,
        IHostEnvironment environment)
    {
        _courseService = courseService;
        _userContext = userContext;
        _assessmentService = assessmentService;
        _environment = environment;
    }

    [HttpPost("evaluate-shadowing")]
    [EnableRateLimiting("pronunciation-ai")]
    [RequestSizeLimit(MaxAudioBytes + 1024 * 64)]
    public async Task<IActionResult> EvaluateShadowing(
        [FromForm] long lessonId,
        [FromForm] long sentenceId,
        [FromForm] int sentenceIndex,
        [FromForm] string? learningMode,
        [FromForm] IFormFile? audio,
        CancellationToken cancellationToken)
    {
        if (audio is null || audio.Length == 0)
        {
            return BadRequest(new { message = "Không nhận được file thu âm." });
        }
        if (audio.Length > MaxAudioBytes)
        {
            return BadRequest(new { message = "File thu âm vượt quá giới hạn 10 MB." });
        }

        var userMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var effectiveMode = _environment.IsDevelopment() && !string.IsNullOrWhiteSpace(learningMode)
            ? learningMode.Trim().ToLowerInvariant()
            : userMode;
        var pronunciationTarget = await _userContext.GetPronunciationTargetAsync(cancellationToken);
        var lessonResult = await _courseService.GetLessonAsync(
            lessonId,
            effectiveMode,
            pronunciationTarget,
            cancellationToken);

        if (lessonResult.Status == LessonLookupStatus.Forbidden) return Forbid();
        if (lessonResult.Lesson is null) return NotFound();

        var sentence = lessonResult.Lesson.Sentences.FirstOrDefault(item => item.SentenceId == sentenceId)
            ?? lessonResult.Lesson.Sentences.ElementAtOrDefault(sentenceIndex);
        if (sentence is null) return BadRequest(new { message = "Câu luyện không hợp lệ." });

        await using var stream = new MemoryStream();
        await audio.CopyToAsync(stream, cancellationToken);
        var audioBytes = stream.ToArray();

        try
        {
            var result = await _assessmentService.AssessAsync(
                new PronunciationAssessmentRequest(
                    audioBytes,
                    GetAudioFormat(audio),
                    sentence.Text,
                    sentence.Ipa,
                    pronunciationTarget),
                cancellationToken);

            return Ok(result);
        }
        catch (PronunciationAssessmentUnavailableException exception)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = exception.Message });
        }
    }

    private static string GetAudioFormat(IFormFile audio)
    {
        var extension = Path.GetExtension(audio.FileName).TrimStart('.').ToLowerInvariant();
        return extension is "mp3" or "wav" ? extension : "wav";
    }
}
