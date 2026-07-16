namespace SpellingBee.Words.Contracts;

public sealed record WordResponse(
    int Id,
    string Text,
    string? PartOfSpeech,
    string? Definition,
    string? Etymology,
    string? Origin,
    string? AudioKey,
    DateTimeOffset ImportedAt);
