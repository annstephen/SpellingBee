using Microsoft.AspNetCore.Http;

namespace SpellingBee.Words.Contracts;

public sealed record ImportWordsRequest(IFormFile File);
