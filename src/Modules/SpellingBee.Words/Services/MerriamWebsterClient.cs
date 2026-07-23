using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpellingBee.Words.Services;

internal sealed partial class MerriamWebsterClient : IMerriamWebsterClient
{
    private const string HeadwordPlaceholder = "_____";

    private readonly HttpClient _httpClient;
    private readonly MerriamWebsterOptions _options;

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex MarkupPattern();

    [GeneratedRegex(@"\{wi\}.*?\{/wi\}")]
    private static partial Regex HeadwordPattern();

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

        var partsOfSpeech = new List<string>();
        foreach (var entry in root.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object) continue;
            if (entry.TryGetProperty("fl", out var entryFl))
            {
                var flValue = entryFl.GetString();
                if (!string.IsNullOrWhiteSpace(flValue) &&
                    !partsOfSpeech.Contains(flValue, StringComparer.OrdinalIgnoreCase))
                {
                    partsOfSpeech.Add(flValue);
                }
            }
        }

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

        string? exampleSentence = null;
        if (first.TryGetProperty("def", out var def))
        {
            var rawVis = FindFirstVisText(def);
            if (rawVis is not null)
                exampleSentence = ObscureExampleSentence(rawVis, GetKnownForms(first, word));
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

        // Inflected forms (e.g. "teeth") have empty shortdef and a cxs cross-reference to the
        // base form. Follow it once to fill in fields the inflected entry is missing.
        if (definition is null &&
            first.TryGetProperty("cxs", out var cxs) && cxs.GetArrayLength() > 0)
        {
            var cx = cxs[0];
            if (cx.TryGetProperty("cxtis", out var cxtis) && cxtis.GetArrayLength() > 0 &&
                cxtis[0].TryGetProperty("cxt", out var cxt))
            {
                var baseWord = cxt.GetString();
                if (baseWord is not null)
                {
                    var baseResult = await LookupAsync(baseWord, ct);
                    definition ??= baseResult?.Definition;
                    etymology ??= baseResult?.Etymology;
                    exampleSentence ??= baseResult?.ExampleSentence;
                    audioKey ??= baseResult?.AudioKey;
                    if (partsOfSpeech.Count == 0 && baseResult?.PartOfSpeech.Count > 0)
                        partsOfSpeech.AddRange(baseResult.PartOfSpeech);
                }
            }
        }

        return new WordLookupResult(partsOfSpeech, definition, etymology, audioKey, exampleSentence);
    }

    // Recursively scans M-W's `def[].sseq` sense tree for the first `["vis", [{ "t": "..." }]]`
    // entry under a `dt` array. A recursive scan is used instead of a hardcoded index path
    // because sense nesting depth varies across headwords/homographs.
    internal static string? FindFirstVisText(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindFirstVisText(item);
                    if (found is not null) return found;
                }
                return null;

            case JsonValueKind.Object:
                if (element.TryGetProperty("dt", out var dt) && dt.ValueKind == JsonValueKind.Array)
                {
                    foreach (var pair in dt.EnumerateArray())
                    {
                        if (pair.ValueKind == JsonValueKind.Array && pair.GetArrayLength() > 1 &&
                            pair[0].ValueKind == JsonValueKind.String &&
                            pair[0].GetString() == "vis" &&
                            pair[1].ValueKind == JsonValueKind.Array &&
                            pair[1].GetArrayLength() > 0 &&
                            pair[1][0].TryGetProperty("t", out var t))
                        {
                            var text = t.GetString();
                            if (!string.IsNullOrWhiteSpace(text)) return text;
                        }
                    }
                }
                foreach (var prop in element.EnumerateObject())
                {
                    var found = FindFirstVisText(prop.Value);
                    if (found is not null) return found;
                }
                return null;

            default:
                return null;
        }
    }

    // Replaces the {wi}headword{/wi} occurrence(s) with a fixed, length-independent placeholder
    // before stripping remaining markup, so other tags ({it}, {b}, ...) elsewhere in the
    // sentence are still cleaned by MarkupPattern. M-W doesn't tag every inflected occurrence
    // of the headword with {wi} (e.g. irregular past tenses, e-drop/consonant-doubling forms
    // are often left untagged), so any remaining known forms are obscured as a fallback pass.
    internal static string ObscureExampleSentence(string rawVisText, IReadOnlyCollection<string>? knownForms = null)
    {
        var obscured = HeadwordPattern().Replace(rawVisText, HeadwordPlaceholder);
        obscured = MarkupPattern().Replace(obscured, string.Empty).Trim();

        if (knownForms is { Count: > 0 })
        {
            var pattern = string.Join('|', knownForms
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(Regex.Escape)
                .OrderByDescending(f => f.Length));
            if (pattern.Length > 0)
                obscured = Regex.Replace(obscured, $@"\b(?:{pattern})\b", HeadwordPlaceholder, RegexOptions.IgnoreCase);
        }

        return obscured;
    }

    // Collects known spellings of the headword (the lookup term, the dictionary's own
    // headword, and any listed inflections) so ObscureExampleSentence can catch occurrences
    // M-W didn't tag with {wi}. Syllable-break markers ('*') are stripped from hw/ins forms.
    internal static IReadOnlyCollection<string> GetKnownForms(JsonElement entry, string word)
    {
        var forms = new List<string> { word };

        if (entry.TryGetProperty("hwi", out var hwi) && hwi.TryGetProperty("hw", out var hw))
        {
            var hwValue = hw.GetString();
            if (hwValue is not null)
                forms.Add(hwValue.Replace("*", string.Empty));
        }

        if (entry.TryGetProperty("ins", out var ins) && ins.ValueKind == JsonValueKind.Array)
        {
            foreach (var inflection in ins.EnumerateArray())
            {
                if (inflection.TryGetProperty("if", out var ifElement))
                {
                    var ifValue = ifElement.GetString();
                    if (ifValue is not null)
                        forms.Add(ifValue.Replace("*", string.Empty));
                }
            }
        }

        return forms;
    }

    internal static string GetAudioSubdir(string key)
    {
        if (key.StartsWith("bix", StringComparison.Ordinal)) return "bix";
        if (key.StartsWith("gg", StringComparison.Ordinal)) return "gg";
        if (char.IsDigit(key[0]) || char.IsPunctuation(key[0])) return "number";
        return key[0].ToString();
    }
}
