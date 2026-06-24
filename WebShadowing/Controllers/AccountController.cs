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
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Thông tin đăng nhập không hợp lệ.";
            return RedirectToAction(nameof(HomeController.Authen), "Home");
        }

        var result = await _authService.LoginAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = result.ErrorMessage;
            return RedirectToAction(nameof(HomeController.Authen), "Home");
        }

        return Redirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Thông tin đăng ký không hợp lệ.";
            return RedirectToAction(nameof(HomeController.Authen), "Home");
        }

        var result = await _authService.RegisterAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AuthError"] = result.ErrorMessage;
            return RedirectToAction(nameof(HomeController.Authen), "Home");
        }

        return Redirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}
