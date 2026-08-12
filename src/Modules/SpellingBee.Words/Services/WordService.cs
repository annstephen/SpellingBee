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
    private readonly ITextToSpeechClient _ttsClient;
    private readonly IAudioFileStore _audioStore;
    private readonly ILogger<WordService> _logger;

    public WordService(
        WordsDbContext db,
        IMerriamWebsterClient mwClient,
        ITextToSpeechClient ttsClient,
        IAudioFileStore audioStore,
        ILogger<WordService> logger)
    {
        _db = db;
        _mwClient = mwClient;
        _ttsClient = ttsClient;
        _audioStore = audioStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WordResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var words = await _db.Words.ToListAsync(ct);
        return words.Select(ToResponse).ToList();
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
        try
        {
            var audioBytes = await _ttsClient.SynthesizeAsync(normalized, ct);
            audioFilePath = await _audioStore.SaveAsync(normalized, audioBytes, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio synthesis failed for '{Word}'", normalized);
        }

        var partOfSpeech = Word.JoinPartsOfSpeech(lookup.PartOfSpeech);
        var word = Word.Create(normalized, partOfSpeech, lookup.Definition, lookup.Etymology, audioFilePath, lookup.ExampleSentence);
        _db.Words.Add(word);
        await _db.SaveChangesAsync(ct);

        return ToResponse(word);
    }

    private static WordResponse ToResponse(Word w) => new(
        w.Id, w.Text, Word.SplitPartsOfSpeech(w.PartOfSpeech), w.Definition, w.Etymology,
        EtymologyOriginParser.ExtractOrigin(w.Etymology), w.ExampleSentence, w.AudioFilePath, w.ImportedAt);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var word = await _db.Words.FindAsync([id], ct);
        if (word is null)
            return false;

        _db.Words.Remove(word);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteManyAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        await _db.Words.Where(w => ids.Contains(w.Id)).ExecuteDeleteAsync(ct);
    }

    public async Task ClearAllAsync(CancellationToken ct = default)
    {
        await _db.Words.ExecuteDeleteAsync(ct);
    }

    public async Task<ExampleSentenceRefreshSummary> RefreshExampleSentencesAsync(CancellationToken ct = default)
    {
        var words = await _db.Words.Where(w => w.ExampleSentence == null).ToListAsync(ct);

        int updated = 0, skipped = 0, failed = 0;
        var failedWords = new List<string>();

        foreach (var word in words)
        {
            ct.ThrowIfCancellationRequested();

            WordLookupResult? lookup;
            try
            {
                lookup = await _mwClient.LookupAsync(word.Text, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "M-W lookup failed for '{Word}'", word.Text);
                failed++;
                failedWords.Add(word.Text);
                continue;
            }

            if (lookup?.ExampleSentence is null)
            {
                skipped++;
                continue;
            }

            word.UpdateExampleSentence(lookup.ExampleSentence);
            updated++;
        }

        await _db.SaveChangesAsync(ct);
        return new ExampleSentenceRefreshSummary(updated, skipped, failed, failedWords);
    }

    public async Task<AudioRegenerateSummary> RegenerateAudioAsync(CancellationToken ct = default)
    {
        var words = await _db.Words.ToListAsync(ct);

        int updated = 0, failed = 0;
        var failedWords = new List<string>();

        foreach (var word in words)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var audioBytes = await _ttsClient.SynthesizeAsync(word.Text, ct);
                var audioFilePath = await _audioStore.SaveAsync(word.Text, audioBytes, ct);
                word.UpdateAudio(audioFilePath);
                updated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audio synthesis failed for '{Word}'", word.Text);
                failed++;
                failedWords.Add(word.Text);
            }
        }

        await _db.SaveChangesAsync(ct);
        return new AudioRegenerateSummary(updated, failed, failedWords);
    }
}
