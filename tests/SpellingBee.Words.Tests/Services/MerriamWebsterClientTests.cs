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
