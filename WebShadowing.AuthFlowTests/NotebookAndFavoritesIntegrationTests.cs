using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebShadowing.Data;
using WebShadowing.Models;
using Xunit;

namespace WebShadowing.AuthFlowTests;

public sealed class NotebookAndFavoritesIntegrationTests
{
    [ConfiguredSqlServerFact]
    public async Task FavoriteSentence_Api_IsIdempotent_AndScopedPerUser()
    {
        using var factory = new AuthFlowApplicationFactory();
        var sentenceId = await SeedSentenceAsync(factory.Services);

        using var alice = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        using var bob = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAndLoginAsync(alice, "Alice", $"alice-{Guid.NewGuid():N}@example.test");
        await RegisterAndLoginAsync(bob, "Bob", $"bob-{Guid.NewGuid():N}@example.test");

        var createPayload = new AddFavoriteRequestDto
        {
            SentenceId = sentenceId,
            Text = "A line worth keeping."
        };

        var createResponse = await alice.PostAsJsonAsync("/api/favorite-sentences", createPayload);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FavoriteSentenceMutationDto>();
        Assert.NotNull(created);
        Assert.False(created.AlreadySaved);
        Assert.True(created.Saved);
        Assert.Equal(FavoriteSourceTypes.LessonSentence, created.Item.SourceType);

        var duplicateResponse = await alice.PostAsJsonAsync("/api/favorite-sentences", createPayload);
        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<FavoriteSentenceMutationDto>();
        Assert.NotNull(duplicate);
        Assert.True(duplicate.AlreadySaved);
        Assert.Equal(created.Item.FavoriteSentenceId, duplicate.Item.FavoriteSentenceId);

        var statusResponse = await alice.GetAsync($"/api/favorite-sentences/status?sentenceId={sentenceId}");
        var status = await statusResponse.Content.ReadFromJsonAsync<FavoriteSentenceStatusDto>();
        Assert.NotNull(status);
        Assert.True(status.IsFavorite);
        Assert.Equal(created.Item.FavoriteSentenceId, status.FavoriteSentenceId);

        var bobList = await bob.GetFromJsonAsync<List<FavoriteSentenceDto>>("/api/favorite-sentences");
        Assert.NotNull(bobList);
        Assert.Empty(bobList);

        var bobDelete = await bob.DeleteAsync($"/api/favorite-sentences/{created.Item.FavoriteSentenceId}");
        Assert.Equal(HttpStatusCode.NotFound, bobDelete.StatusCode);

        var aliceDelete = await alice.DeleteAsync($"/api/favorite-sentences/{created.Item.FavoriteSentenceId}");
        Assert.Equal(HttpStatusCode.NoContent, aliceDelete.StatusCode);

        var deletedStatusResponse = await alice.GetAsync($"/api/favorite-sentences/status?sentenceId={sentenceId}");
        var deletedStatus = await deletedStatusResponse.Content.ReadFromJsonAsync<FavoriteSentenceStatusDto>();
        Assert.NotNull(deletedStatus);
        Assert.False(deletedStatus.IsFavorite);
    }

    private static async Task<long> SeedSentenceAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var course = new Course
        {
            Title = "Favorites Course",
            Level = CourseLevels.Beginner,
            LearningMode = LearningModes.Casual,
            CreatedAt = now,
            UpdatedAt = now,
            Lessons =
            [
                new Lesson
                {
                    Title = "Favorites Lesson",
                    LessonOrder = 1,
                    Duration = 30,
                    Sentences =
                    [
                        new LessonSentence
                        {
                            SentenceOrder = 1,
                            Text = "A line worth keeping.",
                            Translation = "Một câu đáng để lưu."
                        }
                    ]
                }
            ]
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return course.Lessons.Single().Sentences.Single().SentenceId;
    }

    private static async Task RegisterAndLoginAsync(HttpClient client, string fullName, string email)
    {
        const string password = "Shadow123!";
        var registerPage = await client.GetAsync("/Home/Authen?step=register");
        var registerToken = await ReadAntiForgeryTokenAsync(registerPage);
        var registerResponse = await client.PostAsync("/Account/Register", Form(
            ("__RequestVerificationToken", registerToken),
            ("Register.FullName", fullName),
            ("Register.Email", email),
            ("Register.Password", password)));
        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields)
    {
        return new FormUrlEncodedContent(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)));
    }

    private static async Task<string> ReadAntiForgeryTokenAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var start = html.IndexOf("name=\"__RequestVerificationToken\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "The response did not contain an anti-forgery token.");
        var valueStart = html.IndexOf("value=\"", start, StringComparison.Ordinal);
        Assert.True(valueStart >= 0, "The anti-forgery token value was not found.");
        valueStart += 7;
        var valueEnd = html.IndexOf('"', valueStart);
        Assert.True(valueEnd > valueStart, "The anti-forgery token value was incomplete.");
        return WebUtility.HtmlDecode(html[valueStart..valueEnd]);
    }
}