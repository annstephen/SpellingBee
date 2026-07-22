namespace SpellingBee.Words.Contracts;

public sealed record ExampleSentenceRefreshSummary(
    int Updated,
    int Skipped,
    int Failed,
    IReadOnlyList<string> FailedWords);
