using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateProfileAsync(long userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateLearningSettingsAsync(long userId, UpdateLearningSettingsRequestDto request, CancellationToken cancellationToken = default);
}
