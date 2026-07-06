using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

public class HomeController : Controller
{
    private readonly ICourseService _courseService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ICourseService courseService, ILogger<HomeController> logger)
    {
        _courseService = courseService;
        _logger = logger;
    }

    public async Task<IActionResult> Index([FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var effectiveMode = !string.IsNullOrWhiteSpace(mode) ? mode : LearningModes.Casual;
        var normalizedMode = NormalizeMode(effectiveMode);

        var viewModel = new CourseLibraryViewModel
        {
            LearningMode = normalizedMode,
            LearningModeLabel = GetModeLabel(normalizedMode),
            ModeIcon = GetModeIcon(normalizedMode)
        };

        try
        {
            var libraryData = await _courseService.GetLibraryAsync(normalizedMode, cancellationToken);

            viewModel.CurriculumCourses = libraryData.Curriculum.Courses;
            viewModel.VideoBankCourses = libraryData.VideoBank.Courses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load course library data for mode {LearningMode}", normalizedMode);
            viewModel.IsError = true;
            viewModel.ErrorMessage = "Kh\u00f4ng t\u1ea3i \u0111\u01b0\u1ee3c kh\u00f3a h\u1ecdc. Vui l\u00f2ng th\u1eed l\u1ea1i.";
        }

        return View(viewModel);
    }

    public IActionResult Stats()
    {
        return View();
    }

    public IActionResult Settings()
    {
        return View();
    }

    public IActionResult LessonDetail(long id)
    {
        ViewBag.LessonId = id;
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

    private static string NormalizeMode(string mode) => mode.Trim().ToLowerInvariant() switch
    {
        LearningModes.Academic => LearningModes.Academic,
        LearningModes.Professional => LearningModes.Professional,
        _ => LearningModes.Casual
    };

    private static string GetModeLabel(string mode) => mode switch
    {
        LearningModes.Academic => "H\u1ecdc thu\u1eadt",
        LearningModes.Professional => "C\u00f4ng vi\u1ec7c",
        _ => "Giao ti\u1ebfp"
    };

    private static string GetModeIcon(string mode) => mode switch
    {
        LearningModes.Academic => "graduation-cap",
        LearningModes.Professional => "briefcase",
        _ => "compass"
    };
}
