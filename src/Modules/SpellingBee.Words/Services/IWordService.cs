using SpellingBee.Words.Contracts;

namespace SpellingBee.Words.Services;

public interface IWordService
{
    Task<WordResponse> AddWordAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<WordResponse>> GetAllAsync(CancellationToken ct = default);
}
