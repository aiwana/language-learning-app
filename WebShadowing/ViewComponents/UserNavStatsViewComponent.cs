using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;

namespace WebShadowing.ViewComponents;

public class UserNavStatsViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string variant = "full")
    {
        var model = NavDefaults.PreviewNavStats;
        return View(variant == "mobileHeart" ? "MobileHeart" : "Default", model);
    }
}
