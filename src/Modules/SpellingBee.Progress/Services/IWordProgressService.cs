using SpellingBee.Progress.Contracts;

namespace SpellingBee.Progress.Services;

public interface IWordProgressService
{
    Task<WordProgressResponse> RecordAttemptAsync(int wordId, bool wasCorrect, CancellationToken ct);

    Task<IReadOnlyList<WordProgressResponse>> GetAllAsync(CancellationToken ct);

    Task<WordProgressResponse?> GetByWordIdAsync(int wordId, CancellationToken ct);
}
