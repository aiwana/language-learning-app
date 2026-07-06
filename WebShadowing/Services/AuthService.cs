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
        if (!ModelValidation.IsValidEmail(model.Email))
        {
            return AuthResult.Failure("Email không hợp lệ.");
        }

        if (model.Password.Length < 8)
        {
            return AuthResult.Failure("Mật khẩu phải có ít nhất 8 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            return AuthResult.Failure("Vui lòng nhập họ tên.");
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var existingUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return AuthResult.Failure("Email này đã được đăng ký.");
        }

        var usernameBase = BuildUsername(model.FullName, normalizedEmail);
        var username = await GenerateUniqueUsernameAsync(usernameBase, cancellationToken);

        var user = new User
        {
            Username = username,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(null!, model.Password),
            FullName = model.FullName.Trim(),
            LearningMode = LearningModes.Casual,
            PronunciationTarget = PronunciationTargets.Comprehension70,
            Accent = Accents.EnUs,
            IsVip = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        
        var statistics = new UserStatistic
        {
            UserId = user.UserId,
            TotalSessions = 0,
            AverageScore = 0,
            StreakDays = 0,
            Hearts = 5,
            Exp = 0,
            LastPracticeAt = null
        };

        _db.UserStatistics.Add(statistics);
        await _db.SaveChangesAsync(cancellationToken);

        await SignInAsync(user, cancellationToken);
        return AuthResult.Success(user);
    }

    public async Task<AuthResult> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelValidation.IsValidEmail(model.Email))
        {
            return AuthResult.Failure("Email hoặc mật khẩu không đúng.");
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return AuthResult.Failure("Email hoặc mật khẩu không đúng.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return AuthResult.Failure("Email hoặc mật khẩu không đúng.");
        }

        await SignInAsync(user, cancellationToken);
        return AuthResult.Success(user);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task SignInAsync(User user, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("username", user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    private static string BuildUsername(string fullName, string email)
    {
        var baseName = string.Join(string.Empty, fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => part.ToLowerInvariant()));
        return string.IsNullOrWhiteSpace(baseName) ? email.Split('@')[0] : baseName;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        var candidate = baseUsername;
        var suffix = 1;

        while (await _db.Users.AnyAsync(u => u.Username == candidate, cancellationToken))
        {
            candidate = $"{baseUsername}{suffix}";
            suffix++;
        }

        return candidate;
    }
}

public static class ModelValidation
{
    public static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && new System.Net.Mail.MailAddress(email).Address == email.Trim();
    }
}
