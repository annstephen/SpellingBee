namespace SpellingBee.Words.Domain;

public sealed class Word
{
    public const string PartOfSpeechDelimiter = "; ";

    public int Id { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string? PartOfSpeech { get; private set; }
    public string? Definition { get; private set; }
    public string? Etymology { get; private set; }
    public string? ExampleSentence { get; private set; }
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
        string? audioFilePath,
        string? exampleSentence = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return new Word
        {
            Text = text.Trim().ToLowerInvariant(),
            PartOfSpeech = partOfSpeech,
            Definition = definition,
            Etymology = etymology,
            ExampleSentence = exampleSentence,
            AudioKey = audioKey,
            AudioFilePath = audioFilePath,
            ImportedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateExampleSentence(string exampleSentence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleSentence);
        ExampleSentence = exampleSentence;
    }

    public static string? JoinPartsOfSpeech(IReadOnlyList<string> parts) =>
        parts.Count == 0 ? null : string.Join(PartOfSpeechDelimiter, parts);

    public static IReadOnlyList<string> SplitPartsOfSpeech(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(PartOfSpeechDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
