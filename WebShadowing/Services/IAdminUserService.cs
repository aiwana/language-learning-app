using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IAdminUserService
{
    Task<AdminUserSearchResult> SearchUsersAsync(
        string? query,
        string? role,
        bool? isActive,
        bool? isVip,
        int page,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailDto?> GetUserDetailAsync(long userId, CancellationToken cancellationToken = default);

    Task<AdminActionResult> SetActiveAsync(
        long actorUserId,
        long targetUserId,
        bool active,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> GrantVipAsync(
        long actorUserId,
        long targetUserId,
        string billingPeriod,
        int? customDays,
        CancellationToken cancellationToken = default);

    Task<AdminActionResult> RevokeVipAsync(
        long actorUserId,
        long targetUserId,
        CancellationToken cancellationToken = default);
}
