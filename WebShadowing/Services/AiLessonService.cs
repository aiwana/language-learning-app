using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class AiLessonService : IAiLessonService
{
    private readonly AppDbContext _db;
    private readonly ILessonContentService _lessonContent;

    public AiLessonService(AppDbContext db, ILessonContentService lessonContent)
    {
        _db = db;
        _lessonContent = lessonContent;
    }

    /// <summary>
    /// Khung sinh bài — Phase 6C sẽ thay bằng Gemini. Hiện tạo câu thoại từ prompt người dùng.
    /// </summary>
    public GeneratedLessonDto GenerateFromPrompt(string prompt, string level = "Beginner")
    {
        var topic = prompt.Trim();
        var shortTitle = topic.Length > 80 ? topic[..80] + "…" : topic;

        return new GeneratedLessonDto
        {
            Title = shortTitle,
            Level = level,
            Prompt = topic,
            Sentences =
            [
                new LessonSentenceViewModel
                {
                    Text = $"Today we will practice speaking about: {topic}.",
                    Ipa = string.Empty,
                    Translation = $"Hôm nay chúng ta luyện nói về: {topic}."
                },
                new LessonSentenceViewModel
                {
                    Text = "Could you tell me more about this topic?",
                    Ipa = "/kʊd juː tɛl miː mɔːr əˈbaʊt ðɪs ˈtɒpɪk/",
                    Translation = "Bạn có thể nói thêm về chủ đề này không?"
                },
                new LessonSentenceViewModel
                {
                    Text = $"I would like to discuss {topic} in detail.",
                    Ipa = string.Empty,
                    Translation = $"Tôi muốn thảo luận về {topic} chi tiết hơn."
                },
                new LessonSentenceViewModel
                {
                    Text = "Let me summarize the key points clearly.",
                    Ipa = "/lɛt miː ˈsʌməraɪz ðə kiː pɔɪnts ˈklɪrli/",
                    Translation = "Để tôi tóm tắt các ý chính một cách rõ ràng."
                }
            ]
        };
    }

    public async Task<SaveAiLessonResult> SaveDraftAsync(
        long userId,
        GeneratedLessonDto draft,
        CancellationToken cancellationToken = default)
    {
        if (draft.Sentences.Count == 0)
        {
            throw new InvalidOperationException("Bài học AI cần ít nhất một câu thoại.");
        }

        var now = DateTime.UtcNow;
        var aiCourse = await _db.Courses
            .FirstOrDefaultAsync(c =>
                c.CourseType == CourseTypes.AiSaved &&
                c.Title == "Bài học AI của tôi",
                cancellationToken);

        if (aiCourse is null)
        {
            aiCourse = new Course
            {
                Title = "Bài học AI của tôi",
                Description = "Các bài do AI sinh và đã lưu.",
                Level = draft.Level,
                CourseType = CourseTypes.AiSaved,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Courses.Add(aiCourse);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var nextOrder = await _db.Lessons
            .Where(l => l.CourseId == aiCourse.CourseId)
            .MaxAsync(l => (int?)l.LessonOrder, cancellationToken) ?? 0;

        var lesson = new Lesson
        {
            CourseId = aiCourse.CourseId,
            Title = draft.Title,
            Description = draft.Prompt,
            LessonOrder = nextOrder + 1,
            Duration = draft.Sentences.Count * 30,
            Source = LessonSources.Ai,
            CreatedByUserId = userId
        };

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync(cancellationToken);

        var relPath = $"/media/ai/user-{userId}/lesson-{lesson.LessonId}/transcript.txt";
        await _lessonContent.SaveTranscriptAsync(relPath, draft.Sentences, cancellationToken);

        _db.LessonMaterials.Add(new LessonMaterial
        {
            LessonId = lesson.LessonId,
            MaterialType = "transcript",
            ContentUrl = relPath
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new SaveAiLessonResult { LessonId = lesson.LessonId };
    }
}
