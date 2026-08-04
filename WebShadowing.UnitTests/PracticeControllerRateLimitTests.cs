using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class PracticeControllerRateLimitTests : IClassFixture<PracticeControllerRateLimitTests.PracticeApiFactory>
{
    private readonly PracticeApiFactory _factory;

    public PracticeControllerRateLimitTests(PracticeApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EvaluateShadowing_EnforcesPronunciationRateLimit()
    {
        using var client = _factory.CreateClient();

        HttpStatusCode? exceededStatus = null;
        for (var i = 0; i < 11; i++)
        {
            using var form = new MultipartFormDataContent
            {
                { new StringContent("11"), "lessonId" },
                { new StringContent("101"), "sentenceId" },
                { new StringContent("0"), "sentenceIndex" }
            };

            var audio = new ByteArrayContent([1, 2, 3, 4]);
            audio.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(audio, "audio", "sample.wav");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/practice/evaluate-shadowing")
            {
                Content = form
            };
            request.Headers.Add("Idempotency-Key", $"rate-{i}");
            request.Headers.Add("X-Test-User", "rate-limit-user");

            var response = await client.SendAsync(request);
            if (i < 10)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            else
            {
                exceededStatus = response.StatusCode;
            }
        }

        Assert.Equal((HttpStatusCode)429, exceededStatus);
    }

    [Fact]
    public async Task EvaluateShadowing_ReturnsBadRequestWithoutIdempotencyKey()
    {
        using var client = _factory.CreateClient();

        using var form = new MultipartFormDataContent
        {
            { new StringContent("11"), "lessonId" },
            { new StringContent("101"), "sentenceId" },
            { new StringContent("0"), "sentenceIndex" }
        };
        var audio = new ByteArrayContent([1, 2, 3, 4]);
        audio.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(audio, "audio", "sample.wav");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/practice/evaluate-shadowing")
        {
            Content = form
        };
        request.Headers.Add("X-Test-User", "missing-idempotency-user");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public sealed class PracticeApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPracticeEvaluationService>();
                services.AddScoped<IPracticeEvaluationService, FakePracticeEvaluationService>();
                services.RemoveAll<IIpaMatchService>();
                services.AddScoped<IIpaMatchService, FakeIpaMatchService>();

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });
                services.AddAuthorization();
            });
        }
    }

    private sealed class FakePracticeEvaluationService : IPracticeEvaluationService
    {
        public Task<ShadowingEvaluationDto> EvaluateAsync(EvaluateShadowingCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ShadowingEvaluationDto
            {
                Score = 80,
                Passed = true,
                PronunciationTarget = 70,
                Transcript = "hello",
                Feedback = "ok"
            });
        }

        public Task<PracticeAnswerEvaluationDto> EvaluateAnswerAsync(EvaluatePracticeAnswerCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PracticeAnswerEvaluationDto
            {
                Score = 100,
                Passed = true,
                Feedback = "ok"
            });
        }
    }

    private sealed class FakeIpaMatchService : IIpaMatchService
    {
        public Task<IpaMatchQuestionDto> GetQuestionAsync(GetIpaMatchQuestionCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new IpaMatchQuestionDto
            {
                QuestionToken = "token",
                Accent = Accents.EnUs,
                PromptWord = "hello",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                Options =
                [
                    new IpaMatchOptionDto { OptionId = "a", Ipa = "/həˈloʊ/" },
                    new IpaMatchOptionDto { OptionId = "b", Ipa = "/ˈhɛloʊ/" }
                ]
            });
        }

        public Task<PracticeAnswerEvaluationDto> SubmitAnswerAsync(SubmitIpaMatchAnswerCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PracticeAnswerEvaluationDto
            {
                Score = 100,
                Passed = true,
                Feedback = "ok"
            });
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userId = Request.Headers.TryGetValue("X-Test-User", out var headerValues)
                ? headerValues.ToString()
                : "test-user-1";

            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, SchemeName);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
