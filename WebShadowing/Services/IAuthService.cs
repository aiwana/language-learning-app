using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<AuthResult> CompleteOnboardingAsync(long userId, CompleteOnboardingViewModel model, CancellationToken cancellationToken = default);
    Task<UserMeDto?> GetUserAsync(long userId, CancellationToken cancellationToken = default);
}
