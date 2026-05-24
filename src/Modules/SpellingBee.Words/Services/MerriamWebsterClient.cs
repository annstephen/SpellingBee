using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpellingBee.Words.Services;

internal sealed partial class MerriamWebsterClient : IMerriamWebsterClient
{
    private readonly HttpClient _httpClient;
    private readonly MerriamWebsterOptions _options;

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex MarkupPattern();

    public MerriamWebsterClient(HttpClient httpClient, IOptions<MerriamWebsterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<WordLookupResult?> LookupAsync(string word, CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}{Uri.EscapeDataString(word)}?key={_options.ApiKey}";
        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return null;

        var first = root[0];
        // M-W returns an array of suggestion strings when the word isn't found
        if (first.ValueKind == JsonValueKind.String)
            return null;

        var partOfSpeech = first.TryGetProperty("fl", out var fl) ? fl.GetString() : null;

        string? definition = null;
        if (first.TryGetProperty("shortdef", out var shortdef) && shortdef.GetArrayLength() > 0)
            definition = shortdef[0].GetString();

        string? etymology = null;
        if (first.TryGetProperty("et", out var et) && et.GetArrayLength() > 0)
        {
            var etEntry = et[0];
            if (etEntry.ValueKind == JsonValueKind.Array && etEntry.GetArrayLength() > 1)
            {
                var raw = etEntry[1].GetString();
                if (raw is not null)
                    etymology = MarkupPattern().Replace(raw, string.Empty).Trim();
            }
        }

        string? audioKey = null;
        if (first.TryGetProperty("hwi", out var hwi) &&
            hwi.TryGetProperty("prs", out var prs) &&
            prs.GetArrayLength() > 0 &&
            prs[0].TryGetProperty("sound", out var sound) &&
            sound.TryGetProperty("audio", out var audio))
        {
            audioKey = audio.GetString();
        }

        return new WordLookupResult(partOfSpeech, definition, etymology, audioKey);
    }

    internal static string GetAudioSubdir(string key)
    {
        if (key.StartsWith("bix", StringComparison.Ordinal)) return "bix";
        if (key.StartsWith("gg", StringComparison.Ordinal)) return "gg";
        if (char.IsDigit(key[0]) || char.IsPunctuation(key[0])) return "number";
        return key[0].ToString();
    }
}
