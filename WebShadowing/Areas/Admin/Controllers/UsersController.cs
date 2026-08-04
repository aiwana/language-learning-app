using System.Security.Claims;
// Chức năng: MVC admin — danh sách/chi tiết user, disable login, grant/revoke VIP.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebShadowing.Models;
using WebShadowing.Services;

namespace WebShadowing.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = UserRoles.Admin)]
public sealed class UsersController : Controller
{
    private readonly IAdminUserService _adminUsers;

    public UsersController(IAdminUserService adminUsers)
    {
        _adminUsers = adminUsers;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? role,
        string? active,
        string? vip,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        bool? isActive = active switch
        {
            "1" => true,
            "0" => false,
            _ => null
        };
        bool? isVip = vip switch
        {
            "1" => true,
            "0" => false,
            _ => null
        };

        var result = await _adminUsers.SearchUsersAsync(
            q, string.IsNullOrWhiteSpace(role) ? null : role, isActive, isVip, page, 20, cancellationToken);

        ViewBag.Query = q;
        ViewBag.Role = role;
        ViewBag.Active = active;
        ViewBag.Vip = vip;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken = default)
    {
        var detail = await _adminUsers.GetUserDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(long id, string? reason, CancellationToken cancellationToken = default)
    {
        var actorId = GetActorUserId();
        if (actorId is null)
        {
            return Challenge();
        }

        var result = await _adminUsers.SetActiveAsync(actorId.Value, id, active: false, reason, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(long id, CancellationToken cancellationToken = default)
    {
        var actorId = GetActorUserId();
        if (actorId is null)
        {
            return Challenge();
        }

        var result = await _adminUsers.SetActiveAsync(actorId.Value, id, active: true, reason: null, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantVip(
        long id,
        string billingPeriod,
        int? customDays,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorUserId();
        if (actorId is null)
        {
            return Challenge();
        }

        var result = await _adminUsers.GrantVipAsync(actorId.Value, id, billingPeriod, customDays, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeVip(long id, CancellationToken cancellationToken = default)
    {
        var actorId = GetActorUserId();
        if (actorId is null)
        {
            return Challenge();
        }

        var result = await _adminUsers.RevokeVipAsync(actorId.Value, id, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    private long? GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out var id) ? id : null;
    }
}
