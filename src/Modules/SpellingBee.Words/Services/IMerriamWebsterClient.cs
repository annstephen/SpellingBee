namespace SpellingBee.Words.Services;

public interface IMerriamWebsterClient
{
    Task<WordLookupResult?> LookupAsync(string word, CancellationToken ct = default);
}

public sealed record WordLookupResult(
    IReadOnlyList<string> PartOfSpeech,
    string? Definition,
    string? Etymology,
    string? ExampleSentence = null);
