namespace SpellingBee.Words.Infrastructure;

public interface IAudioFileStore
{
    Task<string> SaveAsync(string wordText, byte[] audioBytes, CancellationToken ct = default);
}
