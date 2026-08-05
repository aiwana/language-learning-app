using System.Text.Json;
using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IPaymentService
{
    Task<CheckoutResultDto> CreateCheckoutAsync(long userId, CheckoutRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> HandleMomoWebhookAsync(JsonElement payload, CancellationToken cancellationToken = default);
    Task<bool> HandleZaloPayWebhookAsync(JsonElement payload, CancellationToken cancellationToken = default);
}
