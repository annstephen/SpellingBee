using System.Text.RegularExpressions;

namespace SpellingBee.Words.Services;

internal static partial class EtymologyOriginParser
{
    private static readonly IReadOnlyDictionary<string, string> CanonicalOrigins = new[]
    {
        "New Latin", "Medieval Latin", "Late Latin", "Latin",
        "Middle French", "Old French", "Anglo-French", "French",
        "Middle English", "Old English",
        "Old Norse", "Old High German",
        "Greek", "German", "Dutch", "Spanish", "Italian", "Portuguese",
        "Arabic", "Sanskrit", "Hebrew", "Yiddish", "Japanese", "Chinese",
        "Russian", "Persian", "Hindi", "Malay", "Scandinavian",
        "Swedish", "Norwegian", "Danish", "Welsh", "Irish", "Gaelic",
        "Turkish", "Hawaiian", "Afrikaans",
    }.ToDictionary(name => name, StringComparer.OrdinalIgnoreCase);

    // Compound/modified forms (e.g. "Old French") are listed before their bare form
    // ("French") so the more specific origin wins when both could match.
    [GeneratedRegex(
        @"\b(New Latin|Medieval Latin|Late Latin|Latin|Middle French|Old French|Anglo-French|French|" +
        @"Middle English|Old English|Old Norse|Old High German|Greek|German|Dutch|Spanish|Italian|" +
        @"Portuguese|Arabic|Sanskrit|Hebrew|Yiddish|Japanese|Chinese|Russian|Persian|Hindi|Malay|" +
        @"Scandinavian|Swedish|Norwegian|Danish|Welsh|Irish|Gaelic|Turkish|Hawaiian|Afrikaans)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex OriginPattern();

    /// <summary>
    /// Extracts a single-word (or short-phrase) language of origin from a Merriam-Webster
    /// etymology string, e.g. "Middle French, from Latin cognoscere..." -> "Middle French".
    /// Etymology text is ordered from most immediate to most distant origin, so the first
    /// recognized language name is returned - matching what spelling bees announce.
    /// </summary>
    public static string? ExtractOrigin(string? etymology)
    {
        if (string.IsNullOrWhiteSpace(etymology))
            return null;

        var match = OriginPattern().Match(etymology);
        return match.Success ? CanonicalOrigins[match.Value] : null;
    }
}
