using Microsoft.Extensions.Options;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class TtsAudioService : ITtsAudioService
{
    private readonly IOpenAiApiClient _openAi;
    private readonly AiDialogueOptions _options;
    private readonly IWebHostEnvironment _environment;
    public TtsAudioService(IOpenAiApiClient openAi, IOptions<AiDialogueOptions> options, IWebHostEnvironment environment)
    {
        _openAi = openAi; _options = options.Value; _environment = environment;
    }

    public async Task<string> CreateAsync(string text, string accent, string scope, CancellationToken cancellationToken = default)
    {
        var voice = accent == Accents.EnGb ? _options.TtsVoiceGb : _options.TtsVoiceUs;
        var bytes = await _openAi.CreateSpeechAsync(_options.TtsModel, voice, text, cancellationToken);
        var safeScope = string.Concat(scope.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));
        if (safeScope.Length == 0) safeScope = "audio";
        var relativeDirectory = Path.Combine(_options.AudioLocalPath, safeScope);
        var absoluteDirectory = Path.IsPathRooted(relativeDirectory)
            ? relativeDirectory
            : Path.Combine(_environment.ContentRootPath, relativeDirectory);
        var webRoot = Path.GetFullPath(_environment.WebRootPath);
        var fullDirectory = Path.GetFullPath(absoluteDirectory);
        var webRootPrefix = webRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullDirectory.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ÄÆ°á»ng dáº«n lÆ°u audio pháº£i náº±m trong wwwroot.");
        Directory.CreateDirectory(absoluteDirectory);
        var fileName = $"{Guid.NewGuid():N}.mp3";
        var fullFile = Path.GetFullPath(Path.Combine(absoluteDirectory, fileName));
        await File.WriteAllBytesAsync(fullFile, bytes, cancellationToken);
        return "/" + Path.GetRelativePath(webRoot, fullFile).Replace('\\', '/');
    }
}
