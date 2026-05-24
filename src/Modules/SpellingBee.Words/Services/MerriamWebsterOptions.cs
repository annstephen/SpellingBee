namespace SpellingBee.Words.Services;

public sealed class MerriamWebsterOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.dictionaryapi.com/api/v3/references/collegiate/json/";
    public string AudioBaseUrl { get; set; } = "https://media.merriam-webster.com/audio/prons/en/us/mp3/";
}
