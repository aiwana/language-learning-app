using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
[Route("api/favorite-sentences")]
public sealed class FavoriteSentencesController : ControllerBase
{
    private readonly IFavoriteSentenceService _favoriteSentenceService;
    private readonly IUserContextService _userContextService;

    public FavoriteSentencesController(
        IFavoriteSentenceService favoriteSentenceService,
        IUserContextService userContextService)
    {
        _favoriteSentenceService = favoriteSentenceService;
        _userContextService = userContextService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorites(CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _favoriteSentenceService.GetListAsync(userId.Value, cancellationToken));
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(
        [FromQuery] FavoriteSentenceStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _favoriteSentenceService.GetStatusAsync(userId.Value, request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiErrorDto { ErrorCode = "invalid_favorite_lookup", Message = exception.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveFavorite(
        [FromBody] AddFavoriteRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _favoriteSentenceService.SaveAsync(userId.Value, request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiErrorDto { ErrorCode = "invalid_favorite_request", Message = exception.Message });
        }
    }

    [HttpDelete("{favoriteSentenceId:long}")]
    public async Task<IActionResult> DeleteFavorite(long favoriteSentenceId, CancellationToken cancellationToken)
    {
        var userId = _userContextService.GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return await _favoriteSentenceService.DeleteAsync(userId.Value, favoriteSentenceId, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}