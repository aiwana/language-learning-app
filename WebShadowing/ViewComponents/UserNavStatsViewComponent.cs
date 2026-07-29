using Microsoft.AspNetCore.Mvc;
using WebShadowing.Services;

namespace WebShadowing.ViewComponents;

public class UserNavStatsViewComponent : ViewComponent
{
    private readonly IUserStatsService _statsService;
    private readonly IUserContextService _userContext;

    public UserNavStatsViewComponent(IUserStatsService statsService, IUserContextService userContext)
    {
        _statsService = statsService;
        _userContext = userContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(string variant = "default")
    {
        if (!_userContext.IsAuthenticated)
        {
            return Content(string.Empty);
        }

        var model = await _statsService.GetNavStatsAsync();
        if (model is null)
        {
            return Content(string.Empty);
        }

        return View(
            string.Equals(variant, "mobile", StringComparison.OrdinalIgnoreCase)
                ? "Mobile"
                : "Default",
            model);
    }
}
