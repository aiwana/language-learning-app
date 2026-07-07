using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize]
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
            viewModel.ErrorMessage = "Khong tai duoc khoa hoc. Vui long thu lai.";
        }

        return View(viewModel);
    }

    [Authorize]
    public IActionResult Stats()
    {
        return View();
    }

    [Authorize]
    public IActionResult Settings()
    {
        return View();
    }

    [Authorize]
    public IActionResult LessonDetail(long id)
    {
        ViewBag.LessonId = id;
        return View();
    }

    [AllowAnonymous]
    public IActionResult Authen(string? step)
    {
        ViewData["ActiveStep"] = string.IsNullOrWhiteSpace(step) ? "login" : step;
        return View(new AuthPageViewModel());
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
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
        LearningModes.Academic => "Hoc thuat",
        LearningModes.Professional => "Cong viec",
        _ => "Giao tiep"
    };

    private static string GetModeIcon(string mode) => mode switch
    {
        LearningModes.Academic => "graduation-cap",
        LearningModes.Professional => "briefcase",
        _ => "compass"
    };
}
