using Microsoft.EntityFrameworkCore;
using SpellingBee.Progress.Contracts;
using SpellingBee.Progress.Data;
using SpellingBee.Progress.Domain;

namespace SpellingBee.Progress.Services;

public sealed class WordProgressService : IWordProgressService
{
    private readonly ProgressDbContext _db;

    public WordProgressService(ProgressDbContext db)
    {
        _db = db;
    }

    public async Task<WordProgressResponse> RecordAttemptAsync(int wordId, bool wasCorrect, CancellationToken ct)
    {
        var progress = await _db.WordProgress.FirstOrDefaultAsync(p => p.WordId == wordId, ct);
        if (progress is null)
        {
            progress = WordProgress.Create(wordId);
            _db.WordProgress.Add(progress);
        }

        progress.RecordAttempt(wasCorrect);
        await _db.SaveChangesAsync(ct);

        return ToResponse(progress);
    }

    public async Task<IReadOnlyList<WordProgressResponse>> GetAllAsync(CancellationToken ct)
    {
        var rows = await _db.WordProgress.ToListAsync(ct);
        return rows.Select(ToResponse).ToList();
    }

    public async Task<WordProgressResponse?> GetByWordIdAsync(int wordId, CancellationToken ct)
    {
        var progress = await _db.WordProgress.FirstOrDefaultAsync(p => p.WordId == wordId, ct);
        return progress is null ? null : ToResponse(progress);
    }

    private static WordProgressResponse ToResponse(WordProgress progress) => new(
        progress.WordId,
        progress.AttemptCount,
        progress.CorrectCount,
        progress.IncorrectCount,
        progress.LastAttemptAt);
}
