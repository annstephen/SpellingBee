using SpellingBee.Words.Services;

namespace SpellingBee.Words.Tests.Services;

public sealed class EtymologyOriginParserTests
{
    [Theory]
    [InlineData("Latin cognoscere to become acquainted with, know", "Latin")]
    [InlineData("Greek ephemeros, from epi- + hēmera day", "Greek")]
    [InlineData("Middle French, from Latin conscientia", "Middle French")]
    [InlineData("Old French, from Latin bestia beast", "Old French")]
    [InlineData("Middle English, from Old English", "Middle English")]
    [InlineData("New Latin, from Latin", "New Latin")]
    [InlineData("German Angst", "German")]
    [InlineData("Spanish, from Arabic", "Spanish")]
    public void ExtractOrigin_KnownEtymology_ReturnsLeadingLanguage(string etymology, string expected)
    {
        var result = EtymologyOriginParser.ExtractOrigin(etymology);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("origin unknown")]
    public void ExtractOrigin_NoRecognizedLanguage_ReturnsNull(string? etymology)
    {
        var result = EtymologyOriginParser.ExtractOrigin(etymology);
        Assert.Null(result);
    }
}
