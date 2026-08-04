using System.Globalization;
// Chức năng: adapter thanh toán, xác minh chữ ký/MAC và xử lý webhook idempotent.
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISubscriptionService _subscriptions;
    private readonly PaymentOptions _options;
    public PaymentService(AppDbContext db, IHttpClientFactory httpClientFactory, ISubscriptionService subscriptions, IOptions<PaymentOptions> options)
    { _db = db; _httpClientFactory = httpClientFactory; _subscriptions = subscriptions; _options = options.Value; }

    public async Task<CheckoutResultDto> CreateCheckoutAsync(long userId, CheckoutRequestDto request, CancellationToken cancellationToken = default)
    {
        var provider = request.Provider.Trim().ToLowerInvariant();
        var period = request.BillingPeriod.Trim().ToLowerInvariant();
        if (provider is not (PaymentProviders.Momo or PaymentProviders.ZaloPay)) return new(false, null, null, "Nhà cung cấp thanh toán không hợp lệ.");
        if (period is not (BillingPeriods.Monthly or BillingPeriods.Yearly)) return new(false, null, null, "Chu kỳ thanh toán không hợp lệ.");
        var existing = await _db.PaymentTransactions.AsNoTracking().SingleOrDefaultAsync(
            item => item.Provider == provider && item.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existing is not null) return new(false, null, existing.PaymentTransactionId, "Yêu cầu thanh toán này đã được tạo.");
        var userExists = await _db.Users.AnyAsync(item => item.UserId == userId, cancellationToken);
        if (!userExists) return new(false, null, null, "Không tìm thấy người dùng.");
        if (provider == PaymentProviders.Momo && !MomoConfigured())
            return new(false, null, null, "MoMo chưa được cấu hình merchant credentials.");
        if (provider == PaymentProviders.ZaloPay && !ZaloConfigured())
            return new(false, null, null, "ZaloPay chưa được cấu hình merchant credentials.");
        var amount = period == BillingPeriods.Yearly ? _options.VipYearlyPrice : _options.VipMonthlyPrice;
        var baseOrderId = $"SAI{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..46];
        var orderId = provider == PaymentProviders.ZaloPay
            ? DateTime.UtcNow.ToString("yyMMdd") + baseOrderId[..Math.Min(30, baseOrderId.Length)]
            : baseOrderId;
        var now = DateTime.UtcNow;
        var subscription = new VipSubscription
        {
            UserId = userId, PlanCode = $"vip_{period}", BillingPeriod = period, Status = SubscriptionStatuses.Pending,
            Provider = provider, ProviderSubscriptionId = orderId, StartsAt = now,
            EndsAt = period == BillingPeriods.Yearly ? now.AddYears(1) : now.AddMonths(1), AutoRenew = false,
            CreatedAt = now, UpdatedAt = now
        };
        var transaction = new PaymentTransaction
        {
            UserId = userId, Subscription = subscription, Provider = provider, ProviderTransactionId = orderId,
            IdempotencyKey = request.IdempotencyKey, TransactionType = PaymentTypes.Purchase,
            Status = PaymentStatuses.Pending, Amount = amount, Currency = "VND", CreatedAt = now
        };
        _db.PaymentTransactions.Add(transaction);
        try
        {
            // Persist the merchant order before calling the provider so an
            // immediate IPN can always resolve the order id.
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new(false, null, null, "Yêu cầu thanh toán này đã được tạo.");
        }

        string? payUrl;
        try
        {
            payUrl = provider == PaymentProviders.Momo
                ? await CreateMomoOrderAsync(orderId, amount, cancellationToken)
                : await CreateZaloPayOrderAsync(orderId, userId, amount, cancellationToken);
            if (string.IsNullOrWhiteSpace(payUrl))
                throw new HttpRequestException("Payment provider omitted the checkout URL.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException)
        {
            transaction.Status = PaymentStatuses.Failed;
            transaction.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return new(false, null, transaction.PaymentTransactionId, "Cổng thanh toán chưa phản hồi hợp lệ. Vui lòng thử lại.");
        }
        return new(true, payUrl, transaction.PaymentTransactionId, null);
    }

    public async Task<bool> HandleMomoWebhookAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        if (!MomoConfigured()) return false;
        var get = (string name) => payload.TryGetProperty(name, out var node) ? node.ToString() : string.Empty;
        var raw = $"accessKey={_options.Momo.AccessKey}&amount={get("amount")}&extraData={get("extraData")}&message={get("message")}&orderId={get("orderId")}&orderInfo={get("orderInfo")}&orderType={get("orderType")}&partnerCode={get("partnerCode")}&payType={get("payType")}&requestId={get("requestId")}&responseTime={get("responseTime")}&resultCode={get("resultCode")}&transId={get("transId")}";
        if (!SecureEquals(HmacSha256(_options.Momo.SecretKey, raw), get("signature"))) return false;
        var transaction = await _db.PaymentTransactions.SingleOrDefaultAsync(item => item.Provider == PaymentProviders.Momo && item.ProviderTransactionId == get("orderId"), cancellationToken);
        if (transaction is null || transaction.Amount.ToString("0", CultureInfo.InvariantCulture) != get("amount")) return false;
        if (get("resultCode") != "0")
        {
            transaction.Status = PaymentStatuses.Failed; transaction.ProcessedAt = DateTime.UtcNow; await _db.SaveChangesAsync(cancellationToken); return true;
        }
        await _subscriptions.ActivateAsync(transaction.PaymentTransactionId, get("transId"), DateTime.UtcNow, cancellationToken);
        return true;
    }

    public async Task<bool> HandleZaloPayWebhookAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        if (!ZaloConfigured() || !payload.TryGetProperty("data", out var dataNode) || !payload.TryGetProperty("mac", out var macNode)) return false;
        var data = dataNode.GetString() ?? string.Empty;
        if (!SecureEquals(HmacSha256(_options.ZaloPay.Key2, data), macNode.GetString() ?? string.Empty)) return false;
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;
        var orderId = root.TryGetProperty("app_trans_id", out var orderNode) ? orderNode.GetString() ?? string.Empty : string.Empty;
        var transaction = await _db.PaymentTransactions.SingleOrDefaultAsync(item => item.Provider == PaymentProviders.ZaloPay && item.ProviderTransactionId == orderId, cancellationToken);
        var callbackAmount = root.TryGetProperty("amount", out var amountNode) && amountNode.TryGetDecimal(out var parsedAmount) ? parsedAmount : -1;
        if (transaction is null || callbackAmount != transaction.Amount) return false;
        var providerReference = root.TryGetProperty("zp_trans_id", out var transNode) ? transNode.ToString() : orderId;
        await _subscriptions.ActivateAsync(transaction.PaymentTransactionId, providerReference, DateTime.UtcNow, cancellationToken);
        return true;
    }

    private async Task<string?> CreateMomoOrderAsync(string orderId, decimal amount, CancellationToken cancellationToken)
    {
        if (!MomoConfigured()) throw new InvalidOperationException("MoMo chưa được cấu hình merchant credentials.");
        var value = decimal.ToInt64(amount).ToString(CultureInfo.InvariantCulture);
        var requestId = Guid.NewGuid().ToString("N");
        const string requestType = "captureWallet";
        const string extraData = "";
        const string orderInfo = "ShadowSpeak AI VIP";
        var raw = $"accessKey={_options.Momo.AccessKey}&amount={value}&extraData={extraData}&ipnUrl={_options.Momo.IpnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={_options.Momo.PartnerCode}&redirectUrl={_options.Momo.RedirectUrl}&requestId={requestId}&requestType={requestType}";
        var body = new
        {
            partnerCode = _options.Momo.PartnerCode, requestId, amount = value, orderId, orderInfo,
            redirectUrl = _options.Momo.RedirectUrl, ipnUrl = _options.Momo.IpnUrl, requestType, extraData,
            lang = "vi", signature = HmacSha256(_options.Momo.SecretKey, raw)
        };
        using var response = await _httpClientFactory.CreateClient(nameof(PaymentService)).PostAsJsonAsync(_options.Momo.Endpoint, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.TryGetProperty("payUrl", out var node) ? node.GetString() : null;
    }

    private async Task<string?> CreateZaloPayOrderAsync(string orderId, long userId, decimal amount, CancellationToken cancellationToken)
    {
        if (!ZaloConfigured()) throw new InvalidOperationException("ZaloPay chưa được cấu hình merchant credentials.");
        var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var value = decimal.ToInt64(amount);
        var appTransId = orderId;
        var appUser = userId.ToString(CultureInfo.InvariantCulture);
        const string embedData = "{}";
        const string item = "[]";
        var raw = $"{_options.ZaloPay.AppId}|{appTransId}|{appUser}|{value}|{appTime}|{embedData}|{item}";
        var body = new
        {
            app_id = int.Parse(_options.ZaloPay.AppId, CultureInfo.InvariantCulture), app_user = appUser,
            app_trans_id = appTransId, app_time = appTime, amount = value,
            description = "ShadowSpeak AI VIP", callback_url = _options.ZaloPay.CallbackUrl,
            item, embed_data = embedData, mac = HmacSha256(_options.ZaloPay.Key1, raw)
        };
        using var response = await _httpClientFactory.CreateClient(nameof(PaymentService)).PostAsJsonAsync(_options.ZaloPay.Endpoint, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.TryGetProperty("order_url", out var node) ? node.GetString() : null;
    }

    private bool MomoConfigured() => !string.IsNullOrWhiteSpace(_options.Momo.PartnerCode) && !string.IsNullOrWhiteSpace(_options.Momo.AccessKey)
        && !string.IsNullOrWhiteSpace(_options.Momo.SecretKey) && IsHttpsUrl(_options.Momo.Endpoint)
        && IsHttpsUrl(_options.Momo.IpnUrl) && IsHttpsUrl(_options.Momo.RedirectUrl);
    private bool ZaloConfigured() => int.TryParse(_options.ZaloPay.AppId, out _) && !string.IsNullOrWhiteSpace(_options.ZaloPay.Key1)
        && !string.IsNullOrWhiteSpace(_options.ZaloPay.Key2) && IsHttpsUrl(_options.ZaloPay.Endpoint)
        && IsHttpsUrl(_options.ZaloPay.CallbackUrl);
    private static bool IsHttpsUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
    private static string HmacSha256(string key, string value) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static bool SecureEquals(string expected, string actual)
    {
        var left = Encoding.UTF8.GetBytes(expected.ToLowerInvariant());
        var right = Encoding.UTF8.GetBytes(actual.ToLowerInvariant());
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
