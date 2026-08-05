using Microsoft.AspNetCore.Authorization;
// Chức năng: tra nghĩa, IPA theo batch và tạo/lưu IPA cho câu trong Lesson Detail.
// Phụ trách chính: Minh (IPA matching và dữ liệu tham chiếu ngôn ngữ).
// Minh Anh phối hợp popup tra nghĩa/Shadowing UI.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;
using WebShadowing.Data;
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
    private readonly AppDbContext _db;

    public LanguageReferenceController(
        ILanguageReferenceService languageReferenceService,
        AppDbContext db)
    {
        _languageReferenceService = languageReferenceService;
        _db = db;
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

    [HttpPost("sentence-ipa")]
    [EnableRateLimiting("language-reference-ai")]
    public async Task<IActionResult> GetSentenceIpa(
        [FromBody] SentenceIpaRequestDto request,
        CancellationToken cancellationToken)
    {
        var sentence = await _db.LessonSentences
            .SingleOrDefaultAsync(item => item.SentenceId == request.SentenceId, cancellationToken);
        if (sentence is null) return NotFound(new { message = "Không tìm thấy câu luyện." });
        if (!string.IsNullOrWhiteSpace(sentence.Ipa))
        {
            return Ok(new SentenceIpaDto(sentence.SentenceId, sentence.Ipa));
        }

        var words = Regex.Matches(sentence.Text, @"[\p{L}']+")
            .Select(match => match.Value)
            .Take(40)
            .ToList();
        if (words.Count == 0) return BadRequest(new { message = "Câu luyện không có từ hợp lệ." });

        var entries = await _languageReferenceService.GetIpaBatchAsync(words, cancellationToken);
        var ipaByWord = entries
            .GroupBy(item => NormalizeWord(item.Word))
            .ToDictionary(
                group => group.Key,
                group => group.First().Ipa);
        var tokens = new List<string>(words.Count);
        foreach (var word in words)
        {
            var key = NormalizeWord(word);
            if (!ipaByWord.TryGetValue(key, out var ipa) || string.IsNullOrWhiteSpace(ipa))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { message = "Chưa tạo được phiên âm cho câu này. Vui lòng thử lại." });
            }
            tokens.Add(ipa.Trim().Trim('/'));
        }

        sentence.Ipa = $"/{string.Join(' ', tokens)}/";
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new SentenceIpaDto(sentence.SentenceId, sentence.Ipa));
    }

    private static string NormalizeWord(string word) => new(
        word.ToLowerInvariant().Where(character => char.IsLetter(character) || character == '-').ToArray());
}
