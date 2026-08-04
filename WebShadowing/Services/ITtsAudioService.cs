namespace WebShadowing.Services;

public interface ITtsAudioService
{
    Task<string> CreateAsync(string text, string accent, string scope, CancellationToken cancellationToken = default);
}
