using System.Text.Json;
// Chức năng: checkout VIP, webhook MoMo/ZaloPay và ghi transaction/subscription.
// Phụ trách chính: Minh. Trạng thái: checkout hiện chỉ là demo ở Development/Testing;
// production cố ý trả 503 cho tới khi provider sandbox/webhook end-to-end hoàn thiện.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

[ApiController, Route("api/payment")]
public sealed class PaymentController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly IUserContextService _userContext;
    private readonly AppDbContext _db;
    private readonly PaymentOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PaymentController> _logger;
    public PaymentController(
        IPaymentService service,
        IUserContextService userContext,
        AppDbContext db,
        IOptions<PaymentOptions> options,
        IWebHostEnvironment environment,
        ILogger<PaymentController> logger)
    {
        _service = service;
        _userContext = userContext;
        _db = db;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    [Authorize, HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequestDto request, CancellationToken cancellationToken)
    {
        if (_userContext.GetCurrentUserId() is not long id) return Unauthorized();

        // Temporary product flow: a checkout click activates VIP immediately.
        // It must fail closed in every environment except local development and automated tests.
        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Checkout is unavailable",
                detail: "Demo checkout is disabled outside Development and Testing.");
        }

        if (request.Provider is not (PaymentProviders.Momo or PaymentProviders.ZaloPay)
            || request.BillingPeriod is not (BillingPeriods.Monthly or BillingPeriods.Yearly))
        {
            return BadRequest(new CheckoutResultDto(false, null, null, "Gói thanh toán không hợp lệ."));
        }

        var existingTransaction = await _db.PaymentTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existingTransaction is not null)
        {
            return Ok(new CheckoutResultDto(true, null, existingTransaction.PaymentTransactionId, "VIP đã được kích hoạt."));
        }

        var now = DateTime.UtcNow;
        var subscription = new VipSubscription
        {
            UserId = id,
            PlanCode = $"vip_{request.BillingPeriod}",
            BillingPeriod = request.BillingPeriod,
            Status = SubscriptionStatuses.Active,
            Provider = "demo",
            ProviderSubscriptionId = $"demo-{Guid.NewGuid():N}",
            StartsAt = now,
            EndsAt = request.BillingPeriod == BillingPeriods.Yearly ? now.AddYears(1) : now.AddMonths(1),
            AutoRenew = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        var transaction = new PaymentTransaction
        {
            UserId = id,
            Subscription = subscription,
            Provider = "demo",
            ProviderTransactionId = $"demo-{Guid.NewGuid():N}",
            IdempotencyKey = request.IdempotencyKey,
            TransactionType = PaymentTypes.Purchase,
            Status = PaymentStatuses.Succeeded,
            Amount = request.BillingPeriod == BillingPeriods.Yearly ? _options.VipYearlyPrice : _options.VipMonthlyPrice,
            Currency = "VND",
            CreatedAt = now,
            ProcessedAt = now
        };
        var user = await _db.Users.SingleAsync(item => item.UserId == id, cancellationToken);
        user.IsVip = true;
        _db.VipSubscriptions.Add(subscription);
        _db.PaymentTransactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new CheckoutResultDto(true, null, transaction.PaymentTransactionId, "VIP đã được kích hoạt."));
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
