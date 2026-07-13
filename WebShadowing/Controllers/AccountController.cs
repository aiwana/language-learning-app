using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebShadowing.Data;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;

    public AccountController(IAuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login([Bind(Prefix = "Login")] LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Vui lòng kiểm tra lại thông tin đăng nhập.";
            return RedirectToAction("Authen", "Home", new { step = "login" });
        }

        var result = await _authService.LoginAsync(model);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = result.Message;
            return RedirectToAction("Authen", "Home", new { step = "login" });
        }

        if (result.User?.OnboardingCompleted == true)
        {
            return RedirectToLocal(model.ReturnUrl ?? "/");
        }

        return RedirectToAction("Authen", "Home", new { step = "level" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Vui lòng kiểm tra lại thông tin đăng ký.";
            return RedirectToAction("Authen", "Home", new { step = "level" });
        }

        var result = await _authService.RegisterAsync(model);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = result.Message;
            return RedirectToAction("Authen", "Home", new { step = "level" });
        }

        return RedirectToAction("Authen", "Home", new { step = "level" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Authen", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> CompleteOnboarding(
        OnboardingSelectionViewModel model,
        CancellationToken cancellationToken)
    {
        var validMode = model.LearningMode is LearningModes.Casual
            or LearningModes.Academic
            or LearningModes.Professional;
        var validAccent = model.Accent is Accents.EnUs or Accents.EnGb;
        var validTarget = model.PronunciationTarget is PronunciationTargets.Fluency50
            or PronunciationTargets.Comprehension70
            or PronunciationTargets.Accent90;
        var validPlan = model.Plan is "free" or "vip";

        if (!ModelState.IsValid || !validMode || !validAccent || !validTarget || !validPlan)
        {
            TempData["AuthError"] = "Lựa chọn onboarding không hợp lệ. Vui lòng thử lại.";
            return RedirectToAction("Authen", "Home", new { step = "level" });
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId))
        {
            return Challenge();
        }

        var user = await _db.Users.FirstOrDefaultAsync(
            item => item.UserId == userId,
            cancellationToken);
        if (user is null)
        {
            return Challenge();
        }

        user.LearningMode = model.LearningMode;
        user.Accent = model.Accent;
        user.PronunciationTarget = model.PronunciationTarget;
        // Demo behavior only. Production must derive VIP entitlement from trusted
        // subscription or trial state instead of accepting the client plan value.
        user.IsVip = model.Plan == "vip";
        user.OnboardingCompleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}
