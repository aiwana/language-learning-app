using Microsoft.EntityFrameworkCore;

namespace WebShadowing.Data;

/// <summary>
/// Aligns Lesson_Material URLs in DB with files shipped under wwwroot/media.
/// Safe to run on every startup — only patches known placeholder rows.
/// </summary>
public static class LessonMediaSync
{
    private static readonly Dictionary<string, string> UrlPatches = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/media/beginner/lesson-1/audio.mp3"] = "/media/beginner/lesson-1/audio.wav",
        ["/media/beginner/lesson-2/audio.mp3"] = "/media/beginner/lesson-2/audio.wav",
        ["https://www.youtube.com/watch?v=dQw4w9WgXcQ"] = "https://www.youtube.com/watch?v=epfPE9CP-xo"
    };

    public static async Task SyncAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var materials = await db.LessonMaterials.ToListAsync(cancellationToken);
        var changed = false;

        foreach (var material in materials)
        {
            if (!UrlPatches.TryGetValue(material.ContentUrl, out var patched))
            {
                continue;
            }

            material.ContentUrl = patched;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
