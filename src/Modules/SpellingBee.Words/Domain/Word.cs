namespace SpellingBee.Words.Domain;

public sealed class Word
{
    public int Id { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? PartOfSpeech { get; private set; }
    public string? Definition { get; private set; }
    public string? Etymology { get; private set; }
    public string? AudioKey { get; private set; }
    public string? AudioFilePath { get; private set; }
    public DateTimeOffset ImportedAt { get; private set; }

    private Word() { }

    public static Word Create(
        string text,
        string? partOfSpeech,
        string? definition,
        string? etymology,
        string? audioKey,
        string? audioFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new Word
        {
            Text = text.Trim().ToLowerInvariant(),
            PartOfSpeech = partOfSpeech,
            Definition = definition,
            Etymology = etymology,
            AudioKey = audioKey,
            AudioFilePath = audioFilePath,
            ImportedAt = DateTimeOffset.UtcNow
        };
    }
}
