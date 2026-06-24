using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthResult> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var emailExists = await _db.Users.AnyAsync(
            u => u.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            return new AuthResult(false, "Email này đã được đăng ký.");
        }

        var username = await GenerateUsernameAsync(model.FullName, normalizedEmail, cancellationToken);
        var now = DateTime.UtcNow;

        var user = new User
        {
            Username = username,
            Email = normalizedEmail,
            FullName = model.FullName.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Statistics = new UserStatistic
            {
                TotalSessions = 0,
                AverageScore = 0,
                StreakDays = 0
            }
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await SignInAsync(user);

        return new AuthResult(true);
    }

    public async Task<AuthResult> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return new AuthResult(false, "Email hoặc mật khẩu không đúng.");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return new AuthResult(false, "Email hoặc mật khẩu không đúng.");
        }

        await SignInAsync(user);
        return new AuthResult(true);
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("username", user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available.");

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
    }

    private async Task<string> GenerateUsernameAsync(
        string fullName,
        string email,
        CancellationToken cancellationToken)
    {
        var baseUsername = new string(fullName
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '_')
            .ToArray());

        if (string.IsNullOrWhiteSpace(baseUsername))
        {
            baseUsername = email.Split('@')[0];
        }

        baseUsername = baseUsername[..Math.Min(baseUsername.Length, 40)];
        var username = baseUsername;
        var suffix = 1;

        while (await _db.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            username = $"{baseUsername}{suffix}";
            suffix++;
        }

        return username;
    }
}
