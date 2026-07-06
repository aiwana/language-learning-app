using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;

namespace WebShadowing.Controllers;

public class HomeController : Controller
{
    [Authorize]
    public IActionResult Index()
    {
        return View();
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
}
