using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpellingBee.Words.Contracts;
using SpellingBee.Words.Data;
using SpellingBee.Words.Domain;
using SpellingBee.Words.Infrastructure;
using System.Globalization;

namespace SpellingBee.Words.Services;

internal sealed class WordImportService : IWordImportService
{
    private readonly WordsDbContext _db;
    private readonly IMerriamWebsterClient _mwClient;
    private readonly IAudioFileStore _audioStore;
    private readonly ILogger<WordImportService> _logger;

    public WordImportService(
        WordsDbContext db,
        IMerriamWebsterClient mwClient,
        IAudioFileStore audioStore,
        ILogger<WordImportService> logger)
    {
        _db = db;
        _mwClient = mwClient;
        _audioStore = audioStore;
        _logger = logger;
    }

    public async Task<ImportSummary> ImportAsync(Stream csvStream, CancellationToken ct = default)
    {
        var words = ParseCsv(csvStream);

        int imported = 0, skipped = 0, failed = 0;
        var failedWords = new List<string>();

        foreach (var wordText in words)
        {
            ct.ThrowIfCancellationRequested();

            var normalized = wordText.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized)) continue;

            if (await _db.Words.AnyAsync(w => w.Text == normalized, ct))
            {
                skipped++;
                continue;
            }

            WordLookupResult? lookup;
            try
            {
                lookup = await _mwClient.LookupAsync(normalized, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "M-W lookup failed for '{Word}'", normalized);
                failed++;
                failedWords.Add(normalized);
                continue;
            }

            if (lookup is null)
            {
                _logger.LogWarning("Word '{Word}' not found in Merriam-Webster", normalized);
                failed++;
                failedWords.Add(normalized);
                continue;
            }

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

            try
            {
                await _db.SaveChangesAsync(ct);
                imported++;
            }
            catch (DbUpdateException ex)
            {
                _db.Entry(word).State = EntityState.Detached;
                _logger.LogWarning(ex, "Could not save '{Word}' — likely a duplicate", normalized);
                skipped++;
            }
        }

        return new ImportSummary(imported, skipped, failed, failedWords);
    }

    private static IEnumerable<string> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
        });

        csv.Read();
        csv.ReadHeader();

        if (csv.HeaderRecord is null ||
            !csv.HeaderRecord.Any(h => h.Equals("word", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("CSV must contain a 'word' column header.");
        }

        while (csv.Read())
        {
            var value = csv.GetField<string?>("word");
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }
    }
}
