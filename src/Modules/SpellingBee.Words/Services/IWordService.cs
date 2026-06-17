using SpellingBee.Words.Contracts;

namespace SpellingBee.Words.Services;

public interface IWordService
{
    Task<WordResponse> AddWordAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<WordResponse>> GetAllAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task DeleteManyAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
    Task ClearAllAsync(CancellationToken ct = default);
}
