using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
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

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}
