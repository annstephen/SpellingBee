using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpellingBee.Words.Data;
using SpellingBee.Words.Domain;
using SpellingBee.Words.Infrastructure;
using SpellingBee.Words.Services;

namespace SpellingBee.Words.Tests.Services;

public sealed class WordServiceTests : IDisposable
{
    private readonly WordsDbContext _db;
    private readonly IMerriamWebsterClient _mwClient;
    private readonly ITextToSpeechClient _ttsClient;
    private readonly IAudioFileStore _audioStore;
    private readonly WordService _sut;

    public WordServiceTests()
    {
        var options = new DbContextOptionsBuilder<WordsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new WordsDbContext(options);
        _mwClient = Substitute.For<IMerriamWebsterClient>();
        _ttsClient = Substitute.For<ITextToSpeechClient>();
        _audioStore = Substitute.For<IAudioFileStore>();
        _sut = new WordService(_db, _mwClient, _ttsClient, _audioStore, NullLogger<WordService>.Instance);
    }

    [Fact]
    public async Task AddWordAsync_ValidWord_ReturnsWordResponse()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult(["adjective"], "lasting a very short time", "Greek ephemeros"));
        var audioBytes = new byte[] { 1, 2, 3 };
        _ttsClient.SynthesizeAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(audioBytes);
        _audioStore.SaveAsync("ephemeral", audioBytes, Arg.Any<CancellationToken>())
            .Returns("e/ephemeral.mp3");

        var result = await _sut.AddWordAsync("ephemeral");

        Assert.Equal("ephemeral", result.Text);
        Assert.Equal(["adjective"], result.PartOfSpeech);
        Assert.Equal("lasting a very short time", result.Definition);
        Assert.Equal("e/ephemeral.mp3", result.AudioFilePath);
        Assert.True(result.Id > 0);
        Assert.True(await _db.Words.AnyAsync(w => w.Text == "ephemeral"));
    }

    [Fact]
    public async Task AddWordAsync_NormalisesTextToLowercase()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult([], null, null));

        var result = await _sut.AddWordAsync("  Ephemeral  ");

        Assert.Equal("ephemeral", result.Text);
    }

    [Fact]
    public async Task AddWordAsync_DuplicateWord_ThrowsInvalidOperation()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult([], null, null));
        await _sut.AddWordAsync("ephemeral");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddWordAsync("ephemeral"));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task AddWordAsync_WordNotFoundInMW_ThrowsInvalidOperation()
    {
        _mwClient.LookupAsync("unknownxyz", Arg.Any<CancellationToken>())
            .Returns((WordLookupResult?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddWordAsync("unknownxyz"));

        Assert.Contains("not found in Merriam-Webster", ex.Message);
    }

    [Fact]
    public async Task AddWordAsync_AudioSynthesisFails_StillSavesWord()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult(["adjective"], "a definition", null));
        _ttsClient.SynthesizeAsync("ephemeral", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("audio unavailable"));

        var result = await _sut.AddWordAsync("ephemeral");

        Assert.Equal("ephemeral", result.Text);
        Assert.Null((await _db.Words.SingleAsync(w => w.Text == "ephemeral")).AudioFilePath);
    }

    [Fact]
    public async Task DeleteAsync_ExistingWord_RemovesAndReturnsTrue()
    {
        var word = Word.Create("quorum", null, null, null, null);
        _db.Words.Add(word);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(word.Id);

        Assert.True(result);
        Assert.False(await _db.Words.AnyAsync(w => w.Id == word.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentWord_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(99999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteManyAsync_ExistingWords_RemovesAllMatched()
    {
        var w1 = Word.Create("quorum", null, null, null, null);
        var w2 = Word.Create("zenith", null, null, null, null);
        var w3 = Word.Create("nadir", null, null, null, null);
        _db.Words.AddRange(w1, w2, w3);
        await _db.SaveChangesAsync();

        await _sut.DeleteManyAsync([w1.Id, w2.Id]);

        Assert.False(await _db.Words.AnyAsync(w => w.Id == w1.Id));
        Assert.False(await _db.Words.AnyAsync(w => w.Id == w2.Id));
        Assert.True(await _db.Words.AnyAsync(w => w.Id == w3.Id));
    }

    [Fact]
    public async Task DeleteManyAsync_UnknownIdsIgnored_DoesNotThrow()
    {
        var word = Word.Create("quorum", null, null, null, null);
        _db.Words.Add(word);
        await _db.SaveChangesAsync();

        await _sut.DeleteManyAsync([word.Id, 99999]);

        Assert.False(await _db.Words.AnyAsync(w => w.Id == word.Id));
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllWords()
    {
        _db.Words.AddRange(
            Word.Create("quorum", null, null, null, null),
            Word.Create("zenith", null, null, null, null));
        await _db.SaveChangesAsync();

        await _sut.ClearAllAsync();

        Assert.Equal(0, await _db.Words.CountAsync());
    }

    [Fact]
    public async Task ClearAllAsync_EmptyDatabase_DoesNotThrow()
    {
        await _sut.ClearAllAsync();

        Assert.Equal(0, await _db.Words.CountAsync());
    }

    [Fact]
    public async Task RefreshExampleSentencesAsync_WordMissingSentence_UpdatesIt()
    {
        var word = Word.Create("quorum", null, null, null, null);
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        _mwClient.LookupAsync("quorum", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult(["noun"], "a minimum number of members", null, "The board could not vote without a quorum."));

        var summary = await _sut.RefreshExampleSentencesAsync();

        Assert.Equal(1, summary.Updated);
        Assert.Equal(0, summary.Skipped);
        Assert.Equal(0, summary.Failed);
        Assert.Equal("The board could not vote without a quorum.",
            (await _db.Words.SingleAsync(w => w.Id == word.Id)).ExampleSentence);
    }

    [Fact]
    public async Task RefreshExampleSentencesAsync_WordAlreadyHasSentence_IsNotRefetched()
    {
        var word = Word.Create("quorum", null, null, null, null, "Already has a sentence.");
        _db.Words.Add(word);
        await _db.SaveChangesAsync();

        var summary = await _sut.RefreshExampleSentencesAsync();

        Assert.Equal(0, summary.Updated);
        await _mwClient.DidNotReceive().LookupAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshExampleSentencesAsync_LookupHasNoSentence_CountsAsSkipped()
    {
        var word = Word.Create("quorum", null, null, null, null);
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        _mwClient.LookupAsync("quorum", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult(["noun"], "a minimum number of members", null));

        var summary = await _sut.RefreshExampleSentencesAsync();

        Assert.Equal(0, summary.Updated);
        Assert.Equal(1, summary.Skipped);
        Assert.Null((await _db.Words.SingleAsync(w => w.Id == word.Id)).ExampleSentence);
    }

    [Fact]
    public async Task RefreshExampleSentencesAsync_LookupThrows_CountsAsFailed()
    {
        var word = Word.Create("quorum", null, null, null, null);
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        _mwClient.LookupAsync("quorum", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("unavailable"));

        var summary = await _sut.RefreshExampleSentencesAsync();

        Assert.Equal(0, summary.Updated);
        Assert.Equal(1, summary.Failed);
        Assert.Contains("quorum", summary.FailedWords);
    }

    [Fact]
    public async Task RegenerateAudioAsync_ValidWord_UpdatesAudioFilePath()
    {
        var word = Word.Create("quorum", null, null, null, "old/quorum.mp3");
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        var audioBytes = new byte[] { 1, 2, 3 };
        _ttsClient.SynthesizeAsync("quorum", Arg.Any<CancellationToken>())
            .Returns(audioBytes);
        _audioStore.SaveAsync("quorum", audioBytes, Arg.Any<CancellationToken>())
            .Returns("q/quorum.mp3");

        var summary = await _sut.RegenerateAudioAsync();

        Assert.Equal(1, summary.Updated);
        Assert.Equal(0, summary.Failed);
        Assert.Equal("q/quorum.mp3",
            (await _db.Words.SingleAsync(w => w.Id == word.Id)).AudioFilePath);
    }

    [Fact]
    public async Task RegenerateAudioAsync_SynthesisThrows_CountsAsFailed()
    {
        var word = Word.Create("quorum", null, null, null, null);
        _db.Words.Add(word);
        await _db.SaveChangesAsync();
        _ttsClient.SynthesizeAsync("quorum", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("tts unavailable"));

        var summary = await _sut.RegenerateAudioAsync();

        Assert.Equal(0, summary.Updated);
        Assert.Equal(1, summary.Failed);
        Assert.Contains("quorum", summary.FailedWords);
    }

    public void Dispose() => _db.Dispose();
}
