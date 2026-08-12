namespace SpellingBee.Words.Services;

public interface ITextToSpeechClient
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default);
}
