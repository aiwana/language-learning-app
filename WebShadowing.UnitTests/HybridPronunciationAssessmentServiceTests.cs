using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class HybridPronunciationAssessmentServiceTests
{
    [Fact]
    public async Task AssessAsync_DoesNotFallbackWhenFlagDisabled()
    {
        var options = Options.Create(new PronunciationAssessmentOptions
        {
            EnableOpenAiFallback = false
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-openai-key"
            })
            .Build();

        var httpFactory = new StaticHttpClientFactory(new HttpClient(new FixedResponseHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK))));

        var primary = new AzurePronunciationAssessmentService(
            httpFactory,
            configuration,
            options,
            NullLogger<AzurePronunciationAssessmentService>.Instance);
        var fallback = new OpenAiPronunciationAssessmentService(
            httpFactory,
            configuration,
            options,
            NullLogger<OpenAiPronunciationAssessmentService>.Instance);

        var service = new HybridPronunciationAssessmentService(
            primary,
            fallback,
            options,
            NullLogger<HybridPronunciationAssessmentService>.Instance);

        var exception = await Assert.ThrowsAsync<PronunciationAssessmentUnavailableException>(() =>
            service.AssessAsync(new PronunciationAssessmentRequest(
                BuildWavBytes(1),
                "wav",
                "en-us",
                "casual",
                "hello world",
                null,
                70)));

        Assert.Equal("pronunciation_provider_not_configured", exception.ErrorCode);
    }

    [Fact]
    public async Task AssessAsync_FallsBackWhenFlagEnabled()
    {
        var options = Options.Create(new PronunciationAssessmentOptions
        {
            EnableOpenAiFallback = true,
            OpenAiProviderName = "openai-fallback"
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-openai-key",
                ["OpenAI:AudioModel"] = "gpt-audio"
            })
            .Build();

        var payload = """
        {
          "choices": [
            {
              "message": {
                "content": "{\"score\":78,\"accuracyScore\":76,\"fluencyScore\":80,\"completenessScore\":79,\"prosodyScore\":77,\"transcript\":\"hello world\",\"feedback\":\"ok\",\"words\":[{\"word\":\"hello\",\"accuracyCode\":\"correct\",\"correction\":null,\"phonemes\":[]}]}"
              }
            }
          ]
        }
        """;

        var httpFactory = new StaticHttpClientFactory(new HttpClient(new FixedResponseHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            })));

        var primary = new AzurePronunciationAssessmentService(
            httpFactory,
            configuration,
            options,
            NullLogger<AzurePronunciationAssessmentService>.Instance);
        var fallback = new OpenAiPronunciationAssessmentService(
            httpFactory,
            configuration,
            options,
            NullLogger<OpenAiPronunciationAssessmentService>.Instance);

        var service = new HybridPronunciationAssessmentService(
            primary,
            fallback,
            options,
            NullLogger<HybridPronunciationAssessmentService>.Instance);

        var result = await service.AssessAsync(new PronunciationAssessmentRequest(
            BuildWavBytes(1),
            "wav",
            "en-us",
            "casual",
            "hello world",
            null,
            70));

        Assert.Equal("openai-fallback", result.Provider);
        Assert.Equal(78, result.OverallScore);
        Assert.Single(result.Words);
    }

    private static byte[] BuildWavBytes(int durationSeconds)
    {
        const int sampleRate = 16000;
        const short channels = 1;
        const short bitsPerSample = 16;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var dataSize = byteRate * durationSeconds;
        var totalSize = 44 + dataSize;
        var bytes = new byte[totalSize];

        WriteAscii(bytes, 0, "RIFF");
        BitConverter.GetBytes(totalSize - 8).CopyTo(bytes, 4);
        WriteAscii(bytes, 8, "WAVE");
        WriteAscii(bytes, 12, "fmt ");
        BitConverter.GetBytes(16).CopyTo(bytes, 16);
        BitConverter.GetBytes((short)1).CopyTo(bytes, 20);
        BitConverter.GetBytes(channels).CopyTo(bytes, 22);
        BitConverter.GetBytes(sampleRate).CopyTo(bytes, 24);
        BitConverter.GetBytes(byteRate).CopyTo(bytes, 28);
        BitConverter.GetBytes((short)(channels * bitsPerSample / 8)).CopyTo(bytes, 32);
        BitConverter.GetBytes(bitsPerSample).CopyTo(bytes, 34);
        WriteAscii(bytes, 36, "data");
        BitConverter.GetBytes(dataSize).CopyTo(bytes, 40);

        return bytes;
    }

    private static void WriteAscii(byte[] destination, int offset, string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            destination[offset + i] = (byte)value[i];
        }
    }

    private sealed class FixedResponseHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public FixedResponseHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _factory = factory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_factory(request));
        }
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StaticHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }
}
