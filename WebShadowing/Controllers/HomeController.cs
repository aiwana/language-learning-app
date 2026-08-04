using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

public class HomeController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IUserContextService _userContext;
    private readonly IAuthService _authService;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ICourseService courseService,
        IUserContextService userContext,
        IAuthService authService,
        IHostEnvironment env,
        IConfiguration configuration,
        ILogger<HomeController> logger)
    {
        _courseService = courseService;
        _userContext = userContext;
        _authService = authService;
        _env = env;
        _configuration = configuration;
        _logger = logger;
    }

    [Authorize]
    public async Task<IActionResult> Index([FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var userMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var effectiveMode = _env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode) ? mode : userMode;
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
    public async Task<IActionResult> LessonDetail(long id, [FromQuery] string? mode, CancellationToken cancellationToken)
    {
        var userMode = await _userContext.GetLearningModeAsync(cancellationToken);
        var effectiveMode = _env.IsDevelopment() && !string.IsNullOrWhiteSpace(mode) ? mode : userMode;
        var normalizedMode = NormalizeMode(effectiveMode);
        var pronunciationTarget = await _userContext.GetPronunciationTargetAsync(cancellationToken);
        var lessonResult = await _courseService.GetLessonAsync(
            id,
            normalizedMode,
            pronunciationTarget,
            cancellationToken);

        if (lessonResult.Status == LessonLookupStatus.Forbidden)
        {
            return Forbid();
        }

        if (lessonResult.Lesson is null)
        {
            return NotFound();
        }

        ViewBag.LessonId = id;
        ViewBag.LearningMode = normalizedMode;
        ViewBag.LessonTitle = lessonResult.Lesson.Title;
        ViewBag.PronunciationAiConfigured = !string.IsNullOrWhiteSpace(
            _configuration["OPENAI_API_KEY"] ?? _configuration["OpenAI:ApiKey"]);
        var currentUserId = _userContext.GetCurrentUserId();
        var currentUser = currentUserId is null ? null : await _authService.GetUserAsync(currentUserId.Value, cancellationToken);
        ViewBag.IsVip = currentUser?.IsVip == true;
        ViewBag.InitialLessonJson = JsonSerializer.Serialize(
            lessonResult.Lesson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return View();
    }

    [AllowAnonymous]
    public async Task<IActionResult> Authen(
        string? step,
        string? learningMode,
        string? accent,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        var model = new AuthPageViewModel();
        model.Login.ReturnUrl = safeReturnUrl;
        model.Register.ReturnUrl = safeReturnUrl;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userContext.GetCurrentUserId();
            var profile = userId is null
                ? null
                : await _authService.GetUserAsync(userId.Value, cancellationToken);

            if (profile?.OnboardingCompleted == true)
            {
                return safeReturnUrl is not null
                    ? LocalRedirect(safeReturnUrl)
                    : RedirectToAction(nameof(Index));
            }

            if (profile is null)
            {
                await _authService.LogoutAsync(cancellationToken);
                model.ActiveStep = "login";
            }
            else
            {
                model.ActiveStep = string.Equals(step, "goal", StringComparison.OrdinalIgnoreCase)
                    ? "goal"
                    : "level";
                model.Onboarding.LearningMode = NormalizeOnboardingMode(learningMode, profile.LearningMode);
                model.Onboarding.Accent = NormalizeAccent(accent, profile.Accent);
                model.Onboarding.PronunciationTarget = profile.PronunciationTarget;
                model.Onboarding.Plan = profile.IsVip ? "vip" : "free";
            }
        }
        else
        {
            model.ActiveStep = string.Equals(step, "register", StringComparison.OrdinalIgnoreCase)
                ? "register"
                : "login";
        }

        ViewData["ActiveStep"] = model.ActiveStep;
        ViewData["ReturnUrl"] = safeReturnUrl;
        return View(model);
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

    private static string NormalizeOnboardingMode(string? requestedMode, string storedMode)
    {
        return requestedMode?.Trim().ToLowerInvariant() switch
        {
            LearningModes.Casual => LearningModes.Casual,
            LearningModes.Academic => LearningModes.Academic,
            LearningModes.Professional => LearningModes.Professional,
            _ => NormalizeMode(storedMode)
        };
    }

    private static string NormalizeAccent(string? requestedAccent, string storedAccent)
    {
        return requestedAccent?.Trim().ToLowerInvariant() switch
        {
            Accents.EnUs => Accents.EnUs,
            Accents.EnGb => Accents.EnGb,
            _ => storedAccent == Accents.EnGb ? Accents.EnGb : Accents.EnUs
        };
    }

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
