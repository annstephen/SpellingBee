namespace SpellingBee.Progress.Contracts;

public sealed record WordProgressResponse(
    int WordId,
    int AttemptCount,
    int CorrectCount,
    int IncorrectCount,
    DateTimeOffset? LastAttemptAt);
