using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Route("api")]
public class DictionaryController : ControllerBase
{
    private readonly IDictionaryService _dictionary;

    public DictionaryController(IDictionaryService dictionary)
    {
        _dictionary = dictionary;
    }

    [HttpPost("word-meaning")]
    public async Task<IActionResult> WordMeaning([FromBody] WordMeaningRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Word))
        {
            return BadRequest(new { message = "Word is required." });
        }

        var result = await _dictionary.LookupWordAsync(request.Word, cancellationToken);
        return Ok(new
        {
            word = result.Word,
            ipa = FormatIpaResponse(result.Ipa),
            meaning = result.Meaning
        });
    }

    [HttpPost("word-ipa/batch")]
    public async Task<IActionResult> BatchWordLookup([FromBody] WordBatchRequest request, CancellationToken cancellationToken)
    {
        if (request.Words is not { Count: > 0 })
        {
            return Ok(new WordBatchResponse());
        }

        var lookup = await _dictionary.LookupWordsAsync(request.Words, cancellationToken);
        var response = new WordBatchResponse();

        foreach (var (word, result) in lookup)
        {
            response.Results[word] = new WordLookupResult
            {
                Word = result.Word,
                Ipa = FormatIpaResponse(result.Ipa),
                Meaning = result.Meaning
            };
        }

        return Ok(response);
    }

    private static string FormatIpaResponse(string ipa)
    {
        if (string.IsNullOrWhiteSpace(ipa))
        {
            return string.Empty;
        }

        var core = ipa.Trim().Trim('/');
        return string.IsNullOrEmpty(core) ? string.Empty : $"/{core}/";
    }
}
