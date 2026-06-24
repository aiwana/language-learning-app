using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Models;

namespace WebShadowing.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedCoursesAsync(db, cancellationToken);
        await SeedDemoUserAsync(db, cancellationToken);
    }

    private static async Task SeedCoursesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Courses.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var beginnerCourse = new Course
        {
            Title = "English Shadowing — Beginner",
            Description = "Luyện nói theo phương pháp shadowing với các chủ đề đời sống cơ bản.",
            Level = "Beginner",
            CourseType = CourseTypes.Curriculum,
            CreatedAt = now,
            UpdatedAt = now,
            Lessons =
            [
                new Lesson
                {
                    Title = "Greetings & Introductions",
                    Description = "Chào hỏi và giới thiệu bản thân trong tình huống hàng ngày.",
                    LessonOrder = 1,
                    Duration = 300,
                    Materials =
                    [
                        new LessonMaterial
                        {
                            MaterialType = "audio",
                            ContentUrl = "/media/beginner/lesson-1/audio.wav"
                        },
                        new LessonMaterial
                        {
                            MaterialType = "transcript",
                            ContentUrl = "/media/beginner/lesson-1/transcript.txt"
                        }
                    ]
                },
                new Lesson
                {
                    Title = "At the Coffee Shop",
                    Description = "Gọi món và trò chuyện nhẹ tại quán cà phê.",
                    LessonOrder = 2,
                    Duration = 420,
                    Materials =
                    [
                        new LessonMaterial
                        {
                            MaterialType = "audio",
                            ContentUrl = "/media/beginner/lesson-2/audio.wav"
                        },
                        new LessonMaterial
                        {
                            MaterialType = "text",
                            ContentUrl = "/media/beginner/lesson-2/script.txt"
                        }
                    ]
                }
            ]
        };

        var videoBankCourse = new Course
        {
            Title = "Video Bank — Professional",
            Description = "Video ngắn YouTube cho shadowing — em nạp link sau khi thầy review.",
            Level = "Intermediate",
            CourseType = CourseTypes.VideoBank,
            CreatedAt = now,
            UpdatedAt = now,
            Lessons =
            [
                new Lesson
                {
                    Title = "Job Interview Warm-up",
                    Description = "Luyện shadowing các câu trả lời phỏng vấn thường gặp.",
                    LessonOrder = 1,
                    Duration = 540,
                    Source = LessonSources.Curated,
                    Materials =
                    [
                        new LessonMaterial
                        {
                            MaterialType = "video",
                            ContentUrl = "https://www.youtube.com/watch?v=epfPE9CP-xo"
                        },
                        new LessonMaterial
                        {
                            MaterialType = "transcript",
                            ContentUrl = "/media/intermediate/lesson-1/transcript.txt"
                        }
                    ]
                }
            ]
        };

        db.Courses.AddRange(beginnerCourse, videoBankCourse);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDemoUserAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        const string demoEmail = "demo@shadowspeak.local";
        if (await db.Users.AnyAsync(u => u.Email == demoEmail, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hasher = new PasswordHasher<User>();
        var demoUser = new User
        {
            Username = "demo",
            Email = demoEmail,
            FullName = "Demo Learner",
            CreatedAt = now,
            UpdatedAt = now,
            Statistics = new UserStatistic
            {
                TotalSessions = 12,
                AverageScore = 78.5m,
                StreakDays = 5,
                LastPracticeAt = now.AddDays(-1)
            }
        };

        demoUser.PasswordHash = hasher.HashPassword(demoUser, "Demo@12345");
        db.Users.Add(demoUser);
        await db.SaveChangesAsync(cancellationToken);
    }
}
