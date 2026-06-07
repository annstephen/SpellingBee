using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpellingBee.Words.Contracts;
using SpellingBee.Words.Data;
using SpellingBee.Words.Domain;
using SpellingBee.Words.Infrastructure;

namespace SpellingBee.Words.Services;

internal sealed class WordService : IWordService
{
    private readonly WordsDbContext _db;
    private readonly IMerriamWebsterClient _mwClient;
    private readonly IAudioFileStore _audioStore;
    private readonly ILogger<WordService> _logger;

    public WordService(
        WordsDbContext db,
        IMerriamWebsterClient mwClient,
        IAudioFileStore audioStore,
        ILogger<WordService> logger)
    {
        _db = db;
        _mwClient = mwClient;
        _audioStore = audioStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WordResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Words
            .OrderBy(w => w.Text)
            .Select(w => new WordResponse(w.Id, w.Text, w.PartOfSpeech, w.Definition, w.Etymology, w.AudioKey, w.ImportedAt))
            .ToListAsync(ct);
    }

    public async Task<WordResponse> AddWordAsync(string text, CancellationToken ct = default)
    {
        var normalized = text.Trim().ToLowerInvariant();

        if (await _db.Words.AnyAsync(w => w.Text == normalized, ct))
            throw new InvalidOperationException($"Word '{normalized}' already exists.");

        var lookup = await _mwClient.LookupAsync(normalized, ct);
        if (lookup is null)
            throw new InvalidOperationException($"Word '{normalized}' not found in Merriam-Webster.");

        string? audioFilePath = null;
        if (lookup.AudioKey is not null)
        {
            try
            {
                audioFilePath = await _audioStore.DownloadAsync(lookup.AudioKey, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audio download failed for '{Word}' (key: {Key})", normalized, lookup.AudioKey);
            }
        }

        var word = Word.Create(normalized, lookup.PartOfSpeech, lookup.Definition, lookup.Etymology, lookup.AudioKey, audioFilePath);
        _db.Words.Add(word);
        await _db.SaveChangesAsync(ct);

        return new WordResponse(word.Id, word.Text, word.PartOfSpeech, word.Definition, word.Etymology, word.AudioKey, word.ImportedAt);
    }
}
