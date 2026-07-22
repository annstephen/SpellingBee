namespace SpellingBee.Words.Contracts;

public sealed record WordResponse(
    int Id,
    string Text,
    IReadOnlyList<string> PartOfSpeech,
    string? Definition,
    string? Etymology,
    string? Origin,
    string? ExampleSentence,
    string? AudioKey,
    DateTimeOffset ImportedAt);
