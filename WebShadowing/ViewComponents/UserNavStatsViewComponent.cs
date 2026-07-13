using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.ViewComponents;

public class UserNavStatsViewComponent : ViewComponent
{
    private const string IsVipCacheKey = "UserNavStats.IsVip";
    private readonly AppDbContext _db;

    public UserNavStatsViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(string variant = "full")
    {
        var isVip = await GetCurrentUserVipStatusAsync();
        var preview = NavDefaults.PreviewNavStats;
        var model = new UserNavStatsViewModel
        {
            Streak = preview.Streak,
            Hearts = preview.Hearts,
            Exp = preview.Exp,
            IsVip = isVip
        };

        return View(variant == "mobileHeart" ? "MobileHeart" : "Default", model);
    }

    private async Task<bool> GetCurrentUserVipStatusAsync()
    {
        if (HttpContext.Items.TryGetValue(IsVipCacheKey, out var cachedValue)
            && cachedValue is bool cachedIsVip)
        {
            return cachedIsVip;
        }

        var userIdValue = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isVip = long.TryParse(userIdValue, out var userId)
            && await _db.Users
                .AsNoTracking()
                .Where(user => user.UserId == userId)
                .Select(user => user.IsVip)
                .FirstOrDefaultAsync();

        HttpContext.Items[IsVipCacheKey] = isVip;
        return isVip;
    }
}
