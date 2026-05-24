namespace SpellingBee.Words.Contracts;

public sealed record ImportSummary(
    int Imported,
    int Skipped,
    int Failed,
    IReadOnlyList<string> FailedWords);
