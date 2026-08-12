namespace SpellingBee.Words.Services;

public sealed class GoogleTextToSpeechOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://texttospeech.googleapis.com/v1/text:synthesize";
    public string LanguageCode { get; set; } = "en-US";
    public string VoiceName { get; set; } = "en-US-Neural2-D";
}
