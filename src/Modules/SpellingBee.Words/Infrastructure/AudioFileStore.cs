using Microsoft.Extensions.Options;

namespace SpellingBee.Words.Infrastructure;

internal sealed class AudioFileStore : IAudioFileStore
{
    private readonly string _rootPath;

    public AudioFileStore(IOptions<AudioStorageOptions> storageOptions)
    {
        _rootPath = storageOptions.Value.RootPath;
    }

    public async Task<string> SaveAsync(string wordText, byte[] audioBytes, CancellationToken ct = default)
    {
        var subdir = GetAudioSubdir(wordText);
        var relativePath = $"{subdir}/{wordText}.mp3";
        var localPath = Path.Combine(_rootPath, subdir, $"{wordText}.mp3");

        Directory.CreateDirectory(Path.Combine(_rootPath, subdir));
        await File.WriteAllBytesAsync(localPath, audioBytes, ct);

        return relativePath;
    }

    internal static string GetAudioSubdir(string wordText) =>
        char.IsLetter(wordText[0]) ? wordText[0].ToString().ToLowerInvariant() : "number";
}
