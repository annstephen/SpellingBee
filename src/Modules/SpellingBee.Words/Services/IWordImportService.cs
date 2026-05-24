using SpellingBee.Words.Contracts;

namespace SpellingBee.Words.Services;

public interface IWordImportService
{
    Task<ImportSummary> ImportAsync(Stream csvStream, CancellationToken ct = default);
}
