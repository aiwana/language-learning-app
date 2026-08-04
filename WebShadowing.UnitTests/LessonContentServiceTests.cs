using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;
using Xunit;

namespace WebShadowing.UnitTests;

public sealed class LessonContentServiceTests : IDisposable
{
    private readonly string _webRootPath = Path.Combine(
        Path.GetTempPath(),
        $"webshadowing-content-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task GetSentencesAsync_DoesNotWriteWhenOnlyTranscriptFileExists()
    {
        Directory.CreateDirectory(Path.Combine(_webRootPath, "media"));
        await File.WriteAllTextAsync(
            Path.Combine(_webRootPath, "media", "transcript.json"),
            """
            {
              "sentences": [
                {
                  "sentence_order": 1,
                  "text": "Hello world.",
                  "translation": "Xin chào thế giới.",
                  "ipa": "/həˈloʊ wɝːld/",
                  "start_time": 0.5,
                  "end_time": 1.75
                },
                {
                  "sentence_order": 2,
                  "text": "How are you?"
                }
              ]
            }
            """);

        await using var db = CreateDbContext();
        var service = new LessonContentService(db, new FakeWebHostEnvironment(_webRootPath));
        var materials = new[]
        {
            new LessonMaterial
            {
                LessonId = 11,
                MaterialType = MaterialTypes.Transcript,
                ContentUrl = "/media/transcript.json"
            }
        };

        var result = await service.GetSentencesAsync(11, materials);

        Assert.Equal(2, result.Count);
        Assert.All(result, sentence => Assert.Equal(0, sentence.SentenceId));
        Assert.Empty(await db.LessonSentences.ToListAsync());
        Assert.Equal(0.5, result[0].StartTime);
        Assert.Equal(1.75, result[0].EndTime);
    }

    [Fact]
    public async Task GetSentencesAsync_UsesExistingSentenceIdByOrderWhenPunctuationDiffers()
    {
        Directory.CreateDirectory(Path.Combine(_webRootPath, "media"));
        await File.WriteAllTextAsync(
            Path.Combine(_webRootPath, "media", "transcript.json"),
            """
            {
              "sentences": [
                { "sentence_order": 1, "text": "Hello, world!" }
              ]
            }
            """);

        await using var db = CreateDbContext();
        db.LessonSentences.Add(new LessonSentence
        {
            SentenceId = 101,
            LessonId = 11,
            SentenceOrder = 1,
            Text = "Hello world"
        });
        await db.SaveChangesAsync();

        var service = new LessonContentService(db, new FakeWebHostEnvironment(_webRootPath));
        var result = await service.GetSentencesAsync(
            11,
            [
                new LessonMaterial
                {
                    LessonId = 11,
                    MaterialType = MaterialTypes.Transcript,
                    ContentUrl = "/media/transcript.json"
                }
            ]);

        var sentence = Assert.Single(result);
        Assert.Equal(101, sentence.SentenceId);
        Assert.Equal("Hello, world!", sentence.Text);
        Assert.Single(await db.LessonSentences.ToListAsync());
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRootPath))
        {
            Directory.Delete(_webRootPath, recursive: true);
        }
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string webRootPath)
        {
            WebRootPath = webRootPath;
            ContentRootPath = webRootPath;
            WebRootFileProvider = new PhysicalFileProvider(webRootPath);
            ContentRootFileProvider = WebRootFileProvider;
        }

        public string ApplicationName { get; set; } = "WebShadowing.UnitTests";
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
