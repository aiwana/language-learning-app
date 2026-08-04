using Microsoft.Extensions.Options;
using WebShadowing.Models;

namespace WebShadowing.Services;

public sealed class TtsAudioService : ITtsAudioService
{
    private readonly IOpenAiApiClient _openAi;
    private readonly AiLessonOptions _aiOptions;
    private readonly StorageOptions _storage;
    private readonly IWebHostEnvironment _environment;
    public TtsAudioService(IOpenAiApiClient openAi, IOptions<AiLessonOptions> aiOptions, IOptions<StorageOptions> storage, IWebHostEnvironment environment)
    {
        _openAi = openAi; _aiOptions = aiOptions.Value; _storage = storage.Value; _environment = environment;
    }

    public async Task<string> CreateAsync(string text, string accent, string scope, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(_storage.Provider, "local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage provider hiện chưa được hỗ trợ.");
        var voice = accent == Accents.EnGb ? _aiOptions.TtsVoiceGb : _aiOptions.TtsVoiceUs;
        var bytes = await _openAi.CreateSpeechAsync(_aiOptions.TtsModel, voice, text, cancellationToken);
        var safeScope = string.Concat(scope.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));
        if (safeScope.Length == 0) safeScope = "audio";
        var relativeDirectory = Path.Combine(_storage.LocalPath, safeScope);
        var absoluteDirectory = Path.IsPathRooted(relativeDirectory)
            ? relativeDirectory
            : Path.Combine(_environment.ContentRootPath, relativeDirectory);
        var webRoot = Path.GetFullPath(_environment.WebRootPath);
        var fullDirectory = Path.GetFullPath(absoluteDirectory);
        var webRootPrefix = webRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullDirectory.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn lưu audio phải nằm trong wwwroot.");
        Directory.CreateDirectory(absoluteDirectory);
        var fileName = $"{Guid.NewGuid():N}.mp3";
        var fullFile = Path.GetFullPath(Path.Combine(absoluteDirectory, fileName));
        await File.WriteAllBytesAsync(fullFile, bytes, cancellationToken);
        return "/" + Path.GetRelativePath(webRoot, fullFile).Replace('\\', '/');
    }
}
