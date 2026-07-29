namespace SpellingBee.Progress.Domain;

public sealed class WordProgress
{
    public int Id { get; private set; }
    public int WordId { get; private set; }
    public int AttemptCount { get; private set; }
    public int CorrectCount { get; private set; }
    public int IncorrectCount { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }

    private WordProgress() { }

    public static WordProgress Create(int wordId) => new() { WordId = wordId };

    public void RecordAttempt(bool wasCorrect)
    {
        AttemptCount++;
        if (wasCorrect)
            CorrectCount++;
        else
            IncorrectCount++;
        LastAttemptAt = DateTimeOffset.UtcNow;
    }
}
