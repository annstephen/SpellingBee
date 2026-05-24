using Microsoft.Extensions.Options;
using SpellingBee.Words.Services;

namespace SpellingBee.Words.Infrastructure;

internal sealed class AudioFileStore : IAudioFileStore
{
    private readonly HttpClient _httpClient;
    private readonly string _rootPath;
    private readonly string _audioBaseUrl;

    public AudioFileStore(
        HttpClient httpClient,
        IOptions<AudioStorageOptions> storageOptions,
        IOptions<MerriamWebsterOptions> mwOptions)
    {
        _httpClient = httpClient;
        _rootPath = storageOptions.Value.RootPath;
        _audioBaseUrl = mwOptions.Value.AudioBaseUrl;
    }

    public async Task<string> DownloadAsync(string audioKey, CancellationToken ct = default)
    {
        var subdir = MerriamWebsterClient.GetAudioSubdir(audioKey);
        var relativePath = $"{subdir}/{audioKey}.mp3";
        var localPath = Path.Combine(_rootPath, subdir, $"{audioKey}.mp3");

        if (File.Exists(localPath))
            return relativePath;

        Directory.CreateDirectory(Path.Combine(_rootPath, subdir));

        var url = $"{_audioBaseUrl}{subdir}/{audioKey}.mp3";
        var bytes = await _httpClient.GetByteArrayAsync(url, ct);
        await File.WriteAllBytesAsync(localPath, bytes, ct);

        return relativePath;
    }
}
