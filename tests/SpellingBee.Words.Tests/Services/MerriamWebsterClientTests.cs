using System.Text.Json;
using SpellingBee.Words.Services;

namespace SpellingBee.Words.Tests.Services;

public sealed class MerriamWebsterClientTests
{
    [Theory]
    [InlineData("bixword", "bix")]
    [InlineData("bix", "bix")]
    [InlineData("ggword", "gg")]
    [InlineData("gg", "gg")]
    [InlineData("1test", "number")]
    [InlineData("2abc", "number")]
    [InlineData("ephemeral", "e")]
    [InlineData("aberration", "a")]
    [InlineData("zoology", "z")]
    public void GetAudioSubdir_ReturnsExpectedSubdirectory(string audioKey, string expected)
    {
        var result = MerriamWebsterClient.GetAudioSubdir(audioKey);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ObscureExampleSentence_ReplacesHeadwordWithPlaceholder_AndStripsOtherMarkup()
    {
        var raw = "a {wi}spider{/wi} spun a web {it}quickly{/it}";

        var result = MerriamWebsterClient.ObscureExampleSentence(raw);

        Assert.Equal("a _____ spun a web quickly", result);
    }

    [Fact]
    public void ObscureExampleSentence_MultipleHeadwordOccurrences_ReplacesAll()
    {
        var raw = "the {wi}run{/wi} was long, but she kept {wi}running{/wi}";

        var result = MerriamWebsterClient.ObscureExampleSentence(raw);

        Assert.Equal("the _____ was long, but she kept _____", result);
    }

    [Fact]
    public void ObscureExampleSentence_UntaggedKnownForm_ReplacesIt()
    {
        var raw = "sent a blow to the chin";

        var result = MerriamWebsterClient.ObscureExampleSentence(raw, ["send", "sent"]);

        Assert.Equal("_____ a blow to the chin", result);
    }

    [Fact]
    public void ObscureExampleSentence_TaggedAndUntaggedOccurrences_ReplacesBoth()
    {
        var raw = "she {wi}shuffled{/wi} the cards, then shuffled them again";

        var result = MerriamWebsterClient.ObscureExampleSentence(raw, ["shuffle", "shuffled"]);

        Assert.Equal("she _____ the cards, then _____ them again", result);
    }

    [Fact]
    public void GetKnownForms_CollectsWordHeadwordAndInflections()
    {
        const string json = """
        {
            "hwi": { "hw": "send" },
            "ins": [
                { "if": "sent", "il": "past and past part" }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = MerriamWebsterClient.GetKnownForms(doc.RootElement, "send");

        Assert.Equal(["send", "send", "sent"], result);
    }

    [Fact]
    public void GetKnownForms_StripsSyllableBreakMarkers()
    {
        const string json = """
        {
            "hwi": { "hw": "sep*a*rate" },
            "ins": [
                { "if": "sep*a*rat*ed" }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = MerriamWebsterClient.GetKnownForms(doc.RootElement, "separate");

        Assert.Equal(["separate", "separate", "separated"], result);
    }

    [Fact]
    public void FindFirstVisText_NestedSenseSequence_FindsVisText()
    {
        const string json = """
        {
            "def": [
                {
                    "sseq": [
                        [
                            [
                                "sense",
                                {
                                    "dt": [
                                        ["text", "{sx|foo||}"],
                                        ["vis", [{ "t": "a {wi}spider{/wi} spun a web" }]]
                                    ]
                                }
                            ]
                        ]
                    ]
                }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = MerriamWebsterClient.FindFirstVisText(doc.RootElement.GetProperty("def"));

        Assert.Equal("a {wi}spider{/wi} spun a web", result);
    }

    [Fact]
    public void FindFirstVisText_NoVisEntry_ReturnsNull()
    {
        const string json = """
        {
            "def": [
                {
                    "sseq": [
                        [
                            [
                                "sense",
                                {
                                    "dt": [
                                        ["text", "no example here"]
                                    ]
                                }
                            ]
                        ]
                    ]
                }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(json);

        var result = MerriamWebsterClient.FindFirstVisText(doc.RootElement.GetProperty("def"));

        Assert.Null(result);
    }
}
