using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Infrastructure;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

public class HomeController : Controller
{
    private readonly ILessonService _lessonService;
    private readonly ILessonContentService _lessonContent;
    private readonly IUserContextService _userContext;

    public HomeController(
        ILessonService lessonService,
        ILessonContentService lessonContent,
        IUserContextService userContext)
    {
        _lessonService = lessonService;
        _lessonContent = lessonContent;
        _userContext = userContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var library = await _lessonService.GetLibraryAsync(
            _userContext.GetCurrentUserId(),
            cancellationToken);
        return View(library);
    }

    [Authorize]
    public async Task<IActionResult> LessonPreview(CancellationToken cancellationToken)
    {
        var draft = HttpContext.Session.GetJson<GeneratedLessonDto>("AiLessonDraft");
        if (draft is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new LessonPageViewModel
        {
            LessonId = 0,
            Title = draft.Title,
            Topic = "AI Generator",
            Level = draft.Level,
            IsGenerated = true,
            Sentences = draft.Sentences.ToList()
        };

        await SetUserViewBagsAsync(cancellationToken);
        return View("LessonDetail", model);
    }

    [Authorize]
    public async Task<IActionResult> LessonDetail(long id, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.GetLessonWithDetailsAsync(id, cancellationToken);
        if (lesson is null)
        {
            return NotFound();
        }

        var videoMaterial = lesson.Materials
            .FirstOrDefault(m => m.MaterialType == "video" && m.ContentUrl.Contains("youtube", StringComparison.OrdinalIgnoreCase));

        var audioMaterial = lesson.Materials.FirstOrDefault(m => m.MaterialType == "audio");
        var scriptMaterial = lesson.Materials.FirstOrDefault(m =>
            m.MaterialType is "transcript" or "text");

        var sentences = await _lessonContent.LoadSentencesAsync(scriptMaterial?.ContentUrl, cancellationToken);

        var model = new LessonPageViewModel
        {
            LessonId = lesson.LessonId,
            Title = lesson.Title,
            Topic = lesson.Course.Title,
            Level = lesson.Course.Level,
            IsGenerated = false,
            YoutubeId = ExtractYoutubeId(videoMaterial?.ContentUrl),
            AudioUrl = ResolvePlayableUrl(audioMaterial?.ContentUrl),
            Sentences = sentences.ToList()
        };

        await SetUserViewBagsAsync(cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        await SetUserViewBagsAsync(cancellationToken);
        ViewBag.Flashcards = new List<Flashcard>();
        ViewBag.Favorites = new List<FavoriteSentence>();
        return View();
    }

    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        await SetUserViewBagsAsync(cancellationToken);
        return View();
    }

    public IActionResult Authen(string? returnUrl)
    {
        if (_userContext.IsAuthenticated)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewBag.ReturnUrl = returnUrl ?? Url.Action(nameof(Index)) ?? "/";
        ViewBag.AuthError = TempData["AuthError"];
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task SetUserViewBagsAsync(CancellationToken cancellationToken)
    {
        ViewBag.UserProfile = await _userContext.GetProfileAsync(cancellationToken);
        ViewBag.UserStats = await _userContext.GetStatsAsync(cancellationToken);
        ViewBag.IsAuthenticated = _userContext.IsAuthenticated;
    }

    private static string? ExtractYoutubeId(string? contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(contentUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var path = uri.AbsolutePath;

        // https://www.youtube.com/shorts/VIDEO_ID
        const string shortsPrefix = "/shorts/";
        var shortsIndex = path.IndexOf(shortsPrefix, StringComparison.OrdinalIgnoreCase);
        if (shortsIndex >= 0)
        {
            var id = path[(shortsIndex + shortsPrefix.Length)..].Trim('/');
            var qIndex = id.IndexOf('?');
            return qIndex >= 0 ? id[..qIndex] : id;
        }

        // https://youtu.be/VIDEO_ID
        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return path.Trim('/').Split('?')[0];
        }

        // https://www.youtube.com/watch?v=VIDEO_ID
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var segments = part.Split('=', 2);
            if (segments.Length == 2 &&
                segments[0].Equals("v", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(segments[1]);
            }
        }

        return null;
    }

    private string? ResolvePlayableUrl(string? contentUrl)
    {
        if (string.IsNullOrWhiteSpace(contentUrl))
        {
            return null;
        }

        if (contentUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return contentUrl;
        }

        var path = _lessonContent.ResolveWebRootPath(contentUrl);
        return path is not null && System.IO.File.Exists(path) ? contentUrl : null;
    }
}
