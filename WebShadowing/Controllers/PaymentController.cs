using System.Text.Json;
// Chức năng: checkout VIP và webhook đã xác minh cho MoMo/ZaloPay.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController, Route("api/payment")]
public sealed class PaymentController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly IUserContextService _userContext;
    private readonly ILogger<PaymentController> _logger;
    public PaymentController(
        IPaymentService service,
        IUserContextService userContext,
        ILogger<PaymentController> logger)
    {
        _service = service;
        _userContext = userContext;
        _logger = logger;
    }

    [Authorize, HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequestDto request, CancellationToken cancellationToken)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();

        var result = await _service.CreateCheckoutAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [AllowAnonymous, HttpPost("webhooks/momo")]
    public async Task<IActionResult> MomoWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var accepted = await _service.HandleMomoWebhookAsync(payload, cancellationToken);
        if (!accepted) _logger.LogWarning("Rejected MoMo webhook with invalid signature or transaction data.");
        return accepted ? NoContent() : Unauthorized();
    }

    [AllowAnonymous, HttpPost("webhooks/zalopay")]
    public async Task<IActionResult> ZaloPayWebhook([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        var accepted = await _service.HandleZaloPayWebhookAsync(payload, cancellationToken);
        if (!accepted) _logger.LogWarning("Rejected ZaloPay webhook with invalid MAC or transaction data.");
        return Ok(new { return_code = accepted ? 1 : -1, return_message = accepted ? "success" : "invalid" });
    }
}
