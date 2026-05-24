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
}
