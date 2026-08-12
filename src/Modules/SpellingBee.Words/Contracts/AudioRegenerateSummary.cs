namespace SpellingBee.Words.Contracts;

public sealed record AudioRegenerateSummary(
    int Updated,
    int Failed,
    IReadOnlyList<string> FailedWords);
