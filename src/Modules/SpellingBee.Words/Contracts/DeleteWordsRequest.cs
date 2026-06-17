namespace SpellingBee.Words.Contracts;

public sealed record DeleteWordsRequest(IReadOnlyList<int> Ids);
