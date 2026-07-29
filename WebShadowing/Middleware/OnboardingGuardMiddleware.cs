using Microsoft.AspNetCore.Authorization;
using WebShadowing.Services;

namespace WebShadowing.Middleware;

public sealed class OnboardingGuardMiddleware
{
    private readonly RequestDelegate _next;

    public OnboardingGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        if (ShouldSkip(context) || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var profile = long.TryParse(userIdClaim, out var userId)
            ? await authService.GetUserAsync(userId, context.RequestAborted)
            : null;

        if (profile?.OnboardingCompleted == true)
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Bạn cần hoàn tất onboarding trước khi sử dụng tính năng này.",
                onboardingUrl = "/Home/Authen?step=level"
            }, context.RequestAborted);
            return;
        }

        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"/Home/Authen?step=level&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private static bool ShouldSkip(HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return true;
        }

        var path = context.Request.Path;
        return path.StartsWithSegments("/Home/Authen")
            || path.StartsWithSegments("/Home/Error")
            || path.StartsWithSegments("/Home/Privacy")
            || path.StartsWithSegments("/Account")
            || path.StartsWithSegments("/api/user/me")
            || path.StartsWithSegments("/api/user/onboarding")
            || path.StartsWithSegments("/css")
            || path.StartsWithSegments("/js")
            || path.StartsWithSegments("/lib")
            || path.StartsWithSegments("/images")
            || path.StartsWithSegments("/_framework")
            || path.StartsWithSegments("/favicon");
    }
}
