using Microsoft.EntityFrameworkCore;
// Chức năng: đọc/cập nhật hồ sơ và learning preference cho trang Tài khoản.
// Phụ trách chính: Hải Anh. Minh review validation, auth và các field không được client sửa.
using Microsoft.Extensions.Options;
using WebShadowing.Data;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _db;
    private readonly ModeChangeOptions _modeOptions;
    public UserProfileService(AppDbContext db, IOptions<ModeChangeOptions> modeOptions)
    {
        _db = db;
        _modeOptions = modeOptions.Value;
    }

    public Task<UserProfileDto?> GetAsync(long userId, CancellationToken cancellationToken = default) => BuildAsync(userId, cancellationToken);

    public async Task<UserProfileDto?> UpdateProfileAsync(long userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null) return null;
        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildAsync(userId, cancellationToken);
    }

    public async Task<UserProfileDto?> UpdateLearningSettingsAsync(long userId, UpdateLearningSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.PronunciationTarget is not (50 or 70 or 90)) throw new ArgumentException("Mục tiêu phát âm không hợp lệ.");
        var accent = request.Accent.Trim().ToLowerInvariant();
        if (accent is not (Accents.EnUs or Accents.EnGb)) throw new ArgumentException("Giọng đọc không hợp lệ.");
        var theme = request.Theme.Trim().ToLowerInvariant();
        if (theme is not (ThemePreferences.System or ThemePreferences.Light or ThemePreferences.Dark)) throw new ArgumentException("Giao diện không hợp lệ.");

        var user = await _db.Users.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null) return null;
        var settings = await _db.UserSettings.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (settings is null)
        {
            settings = new UserSettings { UserId = userId, CreatedAt = DateTime.UtcNow };
            _db.UserSettings.Add(settings);
        }
        user.PronunciationTarget = request.PronunciationTarget;
        user.Accent = accent;
        user.UpdatedAt = DateTime.UtcNow;
        settings.AutoSaveAiLessons = request.AutoSaveAiLessons;
        settings.Theme = theme;
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildAsync(userId, cancellationToken);
    }

    private async Task<UserProfileDto?> BuildAsync(long userId, CancellationToken cancellationToken)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var used = await _db.ModeChangeHistory.CountAsync(item => item.UserId == userId && item.ChangedBy == ModeChangeActors.User && item.ChangedAt >= start, cancellationToken);
        return await _db.Users.AsNoTracking().Where(item => item.UserId == userId)
            .Select(user => new UserProfileDto(user.UserId, user.FullName, user.Email, user.Phone,
                user.LearningMode, user.PronunciationTarget, user.Accent, user.IsVip,
                _db.UserSettings.Where(s => s.UserId == userId).Select(s => s.AutoSaveAiLessons).FirstOrDefault(),
                _db.UserSettings.Where(s => s.UserId == userId).Select(s => s.Theme).FirstOrDefault() ?? ThemePreferences.System,
                user.Statistics == null ? 0 : user.Statistics.Exp,
                user.IsVip ? int.MaxValue : Math.Max(0, _modeOptions.FreeChangesPerMonth - used),
                _modeOptions.ExpCostPerChange))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
