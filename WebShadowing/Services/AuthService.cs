using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly VipStubOptions _vipStubOptions;
    private readonly GamificationOptions _gamificationOptions;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor,
        IOptions<VipStubOptions> vipStubOptions,
        IOptions<GamificationOptions> gamificationOptions)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _vipStubOptions = vipStubOptions.Value;
        _gamificationOptions = gamificationOptions.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelValidation.IsValidEmail(model.Email))
        {
            return AuthResult.Failure("Email khÃ´ng há»£p lá»‡.");
        }

        if (model.Password.Length < 8)
        {
            return AuthResult.Failure("Máº­t kháº©u pháº£i cÃ³ Ã­t nháº¥t 8 kÃ½ tá»±.");
        }

        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            return AuthResult.Failure("Vui lÃ²ng nháº­p há» tÃªn.");
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var existingUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return AuthResult.Failure("Email nÃ y Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½.");
        }

        var usernameBase = BuildUsername(model.FullName, normalizedEmail);
        var username = await GenerateUniqueUsernameAsync(usernameBase, cancellationToken);

        var user = new User
        {
            Username = username,
            Email = normalizedEmail,
            FullName = model.FullName.Trim(),
            LearningMode = LearningModes.Casual,
            PronunciationTarget = PronunciationTargets.Comprehension70,
            Accent = Accents.EnUs,
            IsVip = false,
            OnboardingCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        user.Statistics = new UserStatistic
        {
            TotalSessions = 0,
            AverageScore = 0,
            StreakDays = 0,
            Hearts = _gamificationOptions.MaxHearts,
            Exp = 0,
            LastPracticeAt = null
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AuthResult.Failure("KhÃ´ng thá»ƒ táº¡o tÃ i khoáº£n. Email hoáº·c tÃªn ngÆ°á»i dÃ¹ng cÃ³ thá»ƒ Ä‘Ã£ tá»“n táº¡i.");
        }

        await SignInAsync(user, cancellationToken);
        return AuthResult.Success(user);
    }

    public async Task<AuthResult> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelValidation.IsValidEmail(model.Email))
        {
            return AuthResult.Failure("Email hoáº·c máº­t kháº©u khÃ´ng Ä‘Ãºng.");
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return AuthResult.Failure("Email hoáº·c máº­t kháº©u khÃ´ng Ä‘Ãºng.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return AuthResult.Failure("Email hoáº·c máº­t kháº©u khÃ´ng Ä‘Ãºng.");
        }

        if (!user.IsActive)
        {
            return AuthResult.Failure("Tài khoản không khả dụng.");
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

    public async Task<AuthResult> CompleteOnboardingAsync(long userId, CompleteOnboardingViewModel model, CancellationToken cancellationToken = default)
    {
        if (model.LearningMode is not (LearningModes.Casual or LearningModes.Academic or LearningModes.Professional))
        {
            return AuthResult.Failure("HÃ¬nh thá»©c há»c khÃ´ng há»£p lá»‡. Vui lÃ²ng chá»n: giao tiáº¿p, há»c thuáº­t hoáº·c cÃ´ng viá»‡c.");
        }

        if (model.Accent is not (Accents.EnUs or Accents.EnGb))
        {
            return AuthResult.Failure("Chuáº©n phÃ¡t Ã¢m khÃ´ng há»£p lá»‡. Vui lÃ²ng chá»n Anh-Má»¹ hoáº·c Anh-Anh.");
        }

        if (model.PronunciationTarget is not (PronunciationTargets.Fluency50 or PronunciationTargets.Comprehension70 or PronunciationTargets.Accent90))
        {
            return AuthResult.Failure("Má»¥c tiÃªu phÃ¡t Ã¢m khÃ´ng há»£p lá»‡. Vui lÃ²ng chá»n 50, 70 hoáº·c 90.");
        }

        if (model.Plan is not ("free" or "vip"))
        {
            return AuthResult.Failure("GÃ³i tÃ i khoáº£n khÃ´ng há»£p lá»‡.");
        }

        if (model.Plan == "vip" && !_vipStubOptions.Enabled)
        {
            return AuthResult.Failure("KÃ­ch hoáº¡t VIP thá»­ nghiá»‡m hiá»‡n khÃ´ng kháº£ dá»¥ng. Vui lÃ²ng chá»n gÃ³i Miá»…n PhÃ­.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return AuthResult.Failure("KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n.");
        }

        user.LearningMode        = model.LearningMode;
        user.Accent              = model.Accent;
        user.PronunciationTarget = model.PronunciationTarget;
        // This client-selected entitlement is intentionally isolated behind a
        // development stub. Production must replace it with trusted payment or
        // subscription state from the server before enabling VipStub:Enabled.
        user.IsVip               = _vipStubOptions.Enabled && model.Plan == "vip";
        user.OnboardingCompleted = true;
        user.UpdatedAt           = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return AuthResult.Success(user);
    }

    public Task<UserMeDto?> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId)
            .Select(user => new UserMeDto(
                user.UserId,
                user.FullName,
                user.Email,
                user.LearningMode,
                user.PronunciationTarget,
                user.Accent,
                user.IsVip,
                user.OnboardingCompleted,
                user.IsVip && _vipStubOptions.Enabled ? "demo_stub" : "none"))
            .SingleOrDefaultAsync(cancellationToken);
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
