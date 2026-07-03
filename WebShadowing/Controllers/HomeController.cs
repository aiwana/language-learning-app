using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;

namespace WebShadowing.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
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
}
