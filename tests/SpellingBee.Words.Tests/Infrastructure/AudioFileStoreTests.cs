using SpellingBee.Words.Infrastructure;

namespace SpellingBee.Words.Tests.Infrastructure;

public sealed class AudioFileStoreTests
{
    [Theory]
    [InlineData("ephemeral", "e")]
    [InlineData("aberration", "a")]
    [InlineData("zoology", "z")]
    [InlineData("Talcum", "t")]
    public void GetAudioSubdir_LetterLeadingWord_ReturnsLowercasedFirstLetter(string word, string expected)
    {
        var result = AudioFileStore.GetAudioSubdir(word);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("'tis")]
    public void GetAudioSubdir_NonLetterLeadingWord_ReturnsNumberBucket(string word)
    {
        var result = AudioFileStore.GetAudioSubdir(word);
        Assert.Equal("number", result);
    }
}
