using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default);
    Task LogoutAsync();
}

public sealed record AuthResult(bool Succeeded, string? ErrorMessage = null);
