using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUserContextService _userContext;

    public AccountController(IAuthService authService, IUserContextService userContext)
    {
        _authService = authService;
        _userContext = userContext;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [Bind(Prefix = "Login")] LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Vui lòng kiểm tra lại thông tin đăng nhập.";
            return RedirectToAction("Authen", "Home", new { step = "login", returnUrl = LocalReturnUrl(model.ReturnUrl) });
        }

        var result = await _authService.LoginAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = result.Message;
            return RedirectToAction("Authen", "Home", new { step = "login", returnUrl = LocalReturnUrl(model.ReturnUrl) });
        }

        if (result.User?.OnboardingCompleted == false)
        {
            return RedirectToAction("Authen", "Home", new { step = "level", returnUrl = LocalReturnUrl(model.ReturnUrl) });
        }

        return RedirectToLocal(model.ReturnUrl ?? "/");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [Bind(Prefix = "Register")] RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Vui lòng kiểm tra lại thông tin đăng ký.";
            return RedirectToAction("Authen", "Home", new { step = "register", returnUrl = LocalReturnUrl(model.ReturnUrl) });
        }

        var result = await _authService.RegisterAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = result.Message;
            return RedirectToAction("Authen", "Home", new { step = "register", returnUrl = LocalReturnUrl(model.ReturnUrl) });
        }

        return RedirectToAction("Authen", "Home", new { step = "level", returnUrl = LocalReturnUrl(model.ReturnUrl) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> CompleteOnboarding(CompleteOnboardingViewModel model, CancellationToken cancellationToken)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId is null)
        {
            return RedirectToAction("Authen", "Home");
        }

        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Vui lòng chọn hình thức học và mục tiêu phát âm.";
            return RedirectToAction("Authen", "Home", new { step = "level" });
        }

        var result = await _authService.CompleteOnboardingAsync(userId.Value, model, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = result.Message;
            return RedirectToAction("Authen", "Home", new
            {
                step = "goal",
                learningMode = model.LearningMode,
                accent = model.Accent
            });
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Authen", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private string? LocalReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl : null;
    }
}
