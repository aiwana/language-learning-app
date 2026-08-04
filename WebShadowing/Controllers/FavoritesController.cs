using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController, Authorize, Route("api/favorites")]
public sealed class FavoritesController : ControllerBase
{
    private readonly IFavoriteSentenceService _service;
    private readonly IUserContextService _userContext;
    public FavoritesController(IFavoriteSentenceService service, IUserContextService userContext) { _service = service; _userContext = userContext; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => _userContext.GetCurrentUserId() is long id
        ? Ok(await _service.GetListAsync(id, cancellationToken)) : Unauthorized();

    [HttpPost]
    public async Task<IActionResult> Add(AddFavoriteRequestDto request, CancellationToken cancellationToken)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();
        var favorite = await _service.SaveAsync(id, request, cancellationToken);
        return favorite.Item is null ? NotFound() : Ok(favorite);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) =>
        _userContext.GetCurrentUserId() is long userId && await _service.DeleteAsync(userId, id, cancellationToken)
            ? NoContent() : NotFound();
}
