using Microsoft.AspNetCore.Mvc;
using WebShadowing.Services;

namespace WebShadowing.ViewComponents;

public class UserNavStatsViewComponent : ViewComponent
{
    private readonly IUserContextService _userContext;

    public UserNavStatsViewComponent(IUserContextService userContext)
    {
        _userContext = userContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var stats = await _userContext.GetStatsAsync();
        return View(stats);
    }
}
