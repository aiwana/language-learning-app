using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class CourseService : ICourseService
{
    private readonly AppDbContext _db;
    private readonly ILessonContentService _lessonContentService;

    public CourseService(AppDbContext db, ILessonContentService lessonContentService)
    {
        _db = db;
        _lessonContentService = lessonContentService;
    }

    public async Task<LibraryResponseDto> GetLibraryAsync(
        string learningMode,
        CancellationToken cancellationToken = default)
    {
        var courses = await GetCoursesForModeAsync(learningMode, cancellationToken);

        // Batch-load sentence availability for ALL lessons across ALL courses — 1 query total (N+1 fix)
        var allLessonIds = courses.SelectMany(c => c.Lessons).Select(l => l.LessonId);
        var lessonIdsWithSentences = await GetLessonIdsWithDbSentencesAsync(allLessonIds, cancellationToken);

        var courseDtos = new List<CourseLibraryDto>();
        foreach (var course in courses)
        {
            courseDtos.Add(new CourseLibraryDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                Level = course.Level,
                LearningMode = course.LearningMode,
                CourseType = course.CourseType, // Cần chắc chắn mapping DTO có CourseType nếu thư viện UI cần
                Lessons = await BuildLessonSummariesAsync(course.Lessons, lessonIdsWithSentences, cancellationToken)
            });
        }

        return new LibraryResponseDto
        {
            LearningMode = learningMode,
            Curriculum = new LibraryCourseSectionDto
            {
                Courses = courseDtos
                    .Where(course => course.CourseType == CourseTypes.Curriculum)
                    .ToList()
            },
            VideoBank = new LibraryCourseSectionDto
            {
                Courses = courseDtos
                    .Where(course => course.CourseType == CourseTypes.VideoBank)
                    .ToList()
            },
            AiLessons = new AiLessonsSectionDto()
        };
    }

    public async Task<CoursesListResponseDto> GetCoursesAsync(
        string courseType,
        string learningMode,
        CancellationToken cancellationToken = default)
    {
        var courses = await GetCoursesForModeAsync(learningMode, cancellationToken);
        var filteredCourses = courses
            .Where(c => string.Equals(c.CourseType, courseType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Batch-load sentence availability across all filtered lessons — 1 query total (N+1 fix)
        var allLessonIds = filteredCourses.SelectMany(c => c.Lessons).Select(l => l.LessonId);
        var lessonIdsWithSentences = await GetLessonIdsWithDbSentencesAsync(allLessonIds, cancellationToken);

        var items = new List<CourseSummaryDto>();
        foreach (var course in filteredCourses)
        {
            var lessonSummaries = await BuildLessonSummariesAsync(course.Lessons, lessonIdsWithSentences, cancellationToken);
            items.Add(new CourseSummaryDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                Level = course.Level,
                CourseType = course.CourseType,
                LearningMode = course.LearningMode,
                LessonCount = course.Lessons.Count,
                LessonsWithContentCount = lessonSummaries.Count(lesson => lesson.HasContent)
            });
        }

        return new CoursesListResponseDto { Items = items };
    }

    public async Task<CourseDetailDto?> GetCourseAsync(
        long courseId,
        string learningMode,
        CancellationToken cancellationToken = default)
    {
        var course = await _db.Courses
            .AsNoTracking()
            .Include(course => course.Lessons.OrderBy(lesson => lesson.LessonOrder))
                .ThenInclude(lesson => lesson.Materials)
            .FirstOrDefaultAsync(course => course.CourseId == courseId && course.LearningMode == learningMode, cancellationToken);

        if (course is null)
        {
            return null;
        }

        // Batch-load sentence availability in a single query (N+1 fix)
        var lessonIdsWithSentences = await GetLessonIdsWithDbSentencesAsync(
            course.Lessons.Select(l => l.LessonId), cancellationToken);

        return new CourseDetailDto
        {
            CourseId = course.CourseId,
            Title = course.Title,
            Description = course.Description,
            Level = course.Level,
            CourseType = course.CourseType,
            LearningMode = course.LearningMode,
            Lessons = await BuildLessonSummariesAsync(course.Lessons, lessonIdsWithSentences, cancellationToken)
        };
    }

    public async Task<LessonLookupResult> GetLessonAsync(
        long lessonId,
        string learningMode,
        byte pronunciationTarget,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(lesson => lesson.Course)
            .Include(lesson => lesson.Materials)
            .FirstOrDefaultAsync(lesson => lesson.LessonId == lessonId, cancellationToken);

        if (lesson is null)
        {
            return LessonLookupResult.NotFound();
        }

        // Guard against orphaned lessons where the FK navigation fails to populate
        if (lesson.Course is null)
        {
            return LessonLookupResult.NotFound();
        }

        if (lesson.Course.LearningMode != learningMode)
        {
            return LessonLookupResult.Forbidden();
        }

        var materials = lesson.Materials
            .OrderBy(material => material.MaterialId)
            .Select(material => new LessonMaterialDto
            {
                MaterialType = material.MaterialType,
                ContentUrl = material.ContentUrl
            })
            .ToList();

        var sentences = await _lessonContentService.GetSentencesAsync(
            lesson.LessonId,
            lesson.Materials.ToList(),
            cancellationToken);

        return LessonLookupResult.Found(new LessonDetailDto
        {
            LessonId = lesson.LessonId,
            Title = lesson.Title,
            Description = lesson.Description,
            LessonOrder = lesson.LessonOrder,
            Duration = lesson.Duration,
            Source = LessonSources.Curated,
            Course = new LessonCourseDto
            {
                CourseId = lesson.Course.CourseId,
                Title = lesson.Course.Title,
                CourseType = lesson.Course.CourseType,
                LearningMode = lesson.Course.LearningMode,
                Level = lesson.Course.Level
            },
            Materials = materials,
            Media = BuildMedia(lesson.Materials),
            Sentences = sentences,
            PronunciationTarget = pronunciationTarget
        });
    }

    private async Task<List<Course>> GetCoursesForModeAsync(
        string learningMode,
        CancellationToken cancellationToken)
    {
        return await _db.Courses
            .AsNoTracking()
            .Include(course => course.Lessons.OrderBy(lesson => lesson.LessonOrder))
                .ThenInclude(lesson => lesson.Materials)
            .Where(course => course.LearningMode == learningMode)
            .OrderBy(course => course.CourseId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Builds lesson summary DTOs. Accepts a pre-loaded HashSet of lesson IDs that have DB sentences
    /// so that the inner loop only hits the filesystem for lessons not covered by the batch query.
    /// </summary>
    private async Task<IReadOnlyList<LessonSummaryDto>> BuildLessonSummariesAsync(
        IEnumerable<Lesson> lessons,
        HashSet<long> lessonIdsWithDbSentences,
        CancellationToken cancellationToken)
    {
        var summaries = new List<LessonSummaryDto>();

        foreach (var lesson in lessons.OrderBy(lesson => lesson.LessonOrder))
        {
            var materials = lesson.Materials.ToList();
            var hasPlayableMaterial = materials.Any(m =>
                m.MaterialType == MaterialTypes.Video || m.MaterialType == MaterialTypes.Audio);

            // O(1) HashSet lookup — avoids a per-lesson AnyAsync (N+1 fix)
            var hasSentences = lessonIdsWithDbSentences.Contains(lesson.LessonId);
            if (!hasSentences)
            {
                // Only hit the filesystem when the lesson has no DB sentences
                hasSentences = await _lessonContentService.HasTranscriptAsync(materials, cancellationToken);
            }

            summaries.Add(new LessonSummaryDto
            {
                LessonId = lesson.LessonId,
                Title = lesson.Title,
                LessonOrder = lesson.LessonOrder,
                Duration = lesson.Duration,
                Source = LessonSources.Curated,
                HasContent = hasPlayableMaterial && hasSentences,
                MaterialTypes = materials
                    .Select(material => material.MaterialType)
                    .Distinct()
                    .OrderBy(type => type)
                    .ToList()
            });
        }

        return summaries;
    }

    /// <summary>
    /// Returns the set of lesson IDs (from the given list) that have at least one row in Lesson_Sentences.
    /// Executes a single batched SQL query instead of N individual AnyAsync calls.
    /// </summary>
    private async Task<HashSet<long>> GetLessonIdsWithDbSentencesAsync(
        IEnumerable<long> lessonIds,
        CancellationToken cancellationToken)
    {
        var ids = lessonIds.ToList();
        if (ids.Count == 0) return [];

        var result = await _db.LessonSentences
            .AsNoTracking()
            .Where(s => ids.Contains(s.LessonId))
            .Select(s => s.LessonId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. result];
    }

    private static LessonMediaDto BuildMedia(IEnumerable<LessonMaterial> materials)
    {
        var audioUrl = materials
            .Where(material => material.MaterialType == MaterialTypes.Audio)
            .Select(material => material.ContentUrl)
            .FirstOrDefault();

        var videoUrl = materials
            .Where(material => material.MaterialType == MaterialTypes.Video)
            .Select(material => material.ContentUrl)
            .FirstOrDefault();

        return new LessonMediaDto
        {
            YoutubeId = ExtractYoutubeId(videoUrl),
            AudioUrl = audioUrl,
            VideoUrl = videoUrl
        };
    }

    private static string? ExtractYoutubeId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        if (!uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Handle path-based video IDs: /shorts/, /embed/, /live/
        if (uri.AbsolutePath.StartsWith("/shorts/", StringComparison.OrdinalIgnoreCase) ||
            uri.AbsolutePath.StartsWith("/embed/", StringComparison.OrdinalIgnoreCase) ||
            uri.AbsolutePath.StartsWith("/live/", StringComparison.OrdinalIgnoreCase))
        {
            return uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
        }

        // Handle standard watch URL: ?v=VIDEO_ID
        var queryValues = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var queryValue in queryValues)
        {
            var parts = queryValue.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "v")
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        return null;
    }
}
