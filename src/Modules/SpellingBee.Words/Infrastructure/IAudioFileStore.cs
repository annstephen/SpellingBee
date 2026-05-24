namespace SpellingBee.Words.Infrastructure;

public interface IAudioFileStore
{
    Task<string> DownloadAsync(string audioKey, CancellationToken ct = default);
}
