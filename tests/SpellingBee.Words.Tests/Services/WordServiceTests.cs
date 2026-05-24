using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpellingBee.Words.Data;
using SpellingBee.Words.Infrastructure;
using SpellingBee.Words.Services;

namespace SpellingBee.Words.Tests.Services;

public sealed class WordServiceTests : IDisposable
{
    private readonly WordsDbContext _db;
    private readonly IMerriamWebsterClient _mwClient;
    private readonly IAudioFileStore _audioStore;
    private readonly WordService _sut;

    public WordServiceTests()
    {
        var options = new DbContextOptionsBuilder<WordsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new WordsDbContext(options);
        _mwClient = Substitute.For<IMerriamWebsterClient>();
        _audioStore = Substitute.For<IAudioFileStore>();
        _sut = new WordService(_db, _mwClient, _audioStore, NullLogger<WordService>.Instance);
    }

    [Fact]
    public async Task AddWordAsync_ValidWord_ReturnsWordResponse()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult("adjective", "lasting a very short time", "Greek ephemeros", "epheme02"));
        _audioStore.DownloadAsync("epheme02", Arg.Any<CancellationToken>())
            .Returns("e/epheme02.mp3");

        var result = await _sut.AddWordAsync("ephemeral");

        Assert.Equal("ephemeral", result.Text);
        Assert.Equal("adjective", result.PartOfSpeech);
        Assert.Equal("lasting a very short time", result.Definition);
        Assert.Equal("epheme02", result.AudioKey);
        Assert.True(result.Id > 0);
        Assert.True(await _db.Words.AnyAsync(w => w.Text == "ephemeral"));
    }

    [Fact]
    public async Task AddWordAsync_NormalisesTextToLowercase()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult(null, null, null, null));

        var result = await _sut.AddWordAsync("  Ephemeral  ");

        Assert.Equal("ephemeral", result.Text);
    }

    [Fact]
    public async Task AddWordAsync_DuplicateWord_ThrowsInvalidOperation()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult(null, null, null, null));
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
    public async Task AddWordAsync_AudioDownloadFails_StillSavesWord()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult("adjective", "a definition", null, "epheme02"));
        _audioStore.DownloadAsync("epheme02", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("audio unavailable"));

        var result = await _sut.AddWordAsync("ephemeral");

        Assert.Equal("ephemeral", result.Text);
        Assert.Null((await _db.Words.SingleAsync(w => w.Text == "ephemeral")).AudioFilePath);
    }

    public void Dispose() => _db.Dispose();
}
