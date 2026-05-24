namespace SpellingBee.Words.Contracts;

public sealed record WordResponse(
    int Id,
    string Text,
    string? PartOfSpeech,
    string? Definition,
    string? Etymology,
    string? AudioKey,
    DateTimeOffset ImportedAt);
