using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController, Authorize, Route("api/subscription")]
public sealed class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _service;
    private readonly IUserContextService _userContext;
    public SubscriptionController(ISubscriptionService service, IUserContextService userContext) { _service = service; _userContext = userContext; }

    [HttpGet]
    public async Task<IActionResult> Current(CancellationToken cancellationToken) => _userContext.GetCurrentUserId() is long id
        ? Ok(await _service.GetCurrentAsync(id, cancellationToken)) : Unauthorized();

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken) =>
        _userContext.GetCurrentUserId() is long id && await _service.CancelRenewalAsync(id, cancellationToken)
            ? Ok(new { success = true }) : NotFound();
}
