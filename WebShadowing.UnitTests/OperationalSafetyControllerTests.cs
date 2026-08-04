using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebShadowing.Controllers;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class OperationalSafetyControllerTests
{
    [Fact]
    public async Task Health_DoesNotExposeTableCounts()
    {
        await using var db = CreateDbContext();
        var controller = new HealthController(db, TimeProvider.System);

        var result = Assert.IsType<OkObjectResult>(await controller.Get(CancellationToken.None));
        var json = JsonSerializer.Serialize(result.Value);

        Assert.Contains("\"status\":\"healthy\"", json);
        Assert.DoesNotContain("\"users\"", json);
        Assert.DoesNotContain("\"courses\"", json);
    }

    [Fact]
    public async Task Checkout_FailsClosedInProductionBeforeWritingData()
    {
        await using var db = CreateDbContext();
        var controller = new PaymentController(
            new FakePaymentService(),
            new FakeUserContextService(42),
            db,
            Options.Create(new PaymentOptions()),
            new FakeWebHostEnvironment("Production"),
            NullLogger<PaymentController>.Instance);

        var result = Assert.IsType<ObjectResult>(await controller.Checkout(
            new CheckoutRequestDto
            {
                Provider = PaymentProviders.Momo,
                BillingPeriod = BillingPeriods.Monthly,
                IdempotencyKey = "test-key-123"
            },
            CancellationToken.None));

        Assert.Equal(503, result.StatusCode);
        Assert.IsType<ProblemDetails>(result.Value);
        Assert.Empty(await db.PaymentTransactions.ToListAsync());
        Assert.Empty(await db.VipSubscriptions.ToListAsync());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class FakeUserContextService(long userId) : IUserContextService
    {
        public bool IsAuthenticated => true;
        public long? GetCurrentUserId() => userId;
        public Task<string> GetLearningModeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(LearningModes.Casual);
        public Task<byte> GetPronunciationTargetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((byte)70);
        public Task<string> GetAccentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Accents.EnUs);
    }

    private sealed class FakePaymentService : IPaymentService
    {
        public Task<CheckoutResultDto> CreateCheckoutAsync(
            long userId,
            CheckoutRequestDto request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Production demo checkout must not invoke the payment service.");

        public Task<bool> HandleMomoWebhookAsync(
            JsonElement payload,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HandleZaloPayWebhookAsync(
            JsonElement payload,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakeWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "WebShadowing.UnitTests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
