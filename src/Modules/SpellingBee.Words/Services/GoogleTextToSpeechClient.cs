using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace SpellingBee.Words.Services;

internal sealed class GoogleTextToSpeechClient : ITextToSpeechClient
{
    private readonly HttpClient _httpClient;
    private readonly GoogleTextToSpeechOptions _options;

    public GoogleTextToSpeechClient(HttpClient httpClient, IOptions<GoogleTextToSpeechOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}?key={Uri.EscapeDataString(_options.ApiKey)}";
        var requestBody = new
        {
            input = new { text },
            voice = new { languageCode = _options.LanguageCode, name = _options.VoiceName },
            audioConfig = new { audioEncoding = "MP3" }
        };

        using var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var audioContent = doc.RootElement.GetProperty("audioContent").GetString();
        if (string.IsNullOrEmpty(audioContent))
            throw new InvalidOperationException("Google Text-to-Speech response did not contain audio content.");

        return Convert.FromBase64String(audioContent);
    }
}
