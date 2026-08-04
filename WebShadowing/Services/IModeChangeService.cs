using WebShadowing.Models;

namespace WebShadowing.Services;

public interface IModeChangeService
{
    Task<ModeChangeResultDto> ChangeAsync(long userId, ChangeLearningModeRequestDto request, CancellationToken cancellationToken = default);
}
