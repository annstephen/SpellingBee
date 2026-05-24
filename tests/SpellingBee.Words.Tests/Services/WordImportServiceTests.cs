using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpellingBee.Words.Data;
using SpellingBee.Words.Infrastructure;
using SpellingBee.Words.Services;
using System.Text;

namespace SpellingBee.Words.Tests.Services;

public sealed class WordImportServiceTests : IDisposable
{
    private readonly WordsDbContext _db;
    private readonly IMerriamWebsterClient _mwClient;
    private readonly IAudioFileStore _audioStore;
    private readonly WordImportService _sut;

    public WordImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<WordsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new WordsDbContext(options);
        _mwClient = Substitute.For<IMerriamWebsterClient>();
        _audioStore = Substitute.For<IAudioFileStore>();
        _sut = new WordImportService(_db, _mwClient, _audioStore, NullLogger<WordImportService>.Instance);
    }

    [Fact]
    public async Task ImportAsync_ValidWord_CountsAsImported()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult("adjective", "lasting a very short time", "Greek ephemeros", "epheme02"));
        _audioStore.DownloadAsync("epheme02", Arg.Any<CancellationToken>())
            .Returns("e/epheme02.mp3");

        var summary = await _sut.ImportAsync(ToCsvStream("ephemeral"));

        Assert.Equal(1, summary.Imported);
        Assert.Equal(0, summary.Skipped);
        Assert.Equal(0, summary.Failed);
        Assert.True(await _db.Words.AnyAsync(w => w.Text == "ephemeral"));
    }

    [Fact]
    public async Task ImportAsync_DuplicateWord_CountsAsSkipped()
    {
        _mwClient.LookupAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult("adjective", "a definition", null, null));

        await _sut.ImportAsync(ToCsvStream("ephemeral"));
        var summary = await _sut.ImportAsync(ToCsvStream("ephemeral"));

        Assert.Equal(0, summary.Imported);
        Assert.Equal(1, summary.Skipped);
        Assert.Equal(0, summary.Failed);
    }

    [Fact]
    public async Task ImportAsync_WordNotFoundInMW_CountsAsFailed()
    {
        _mwClient.LookupAsync("unknownxyz", Arg.Any<CancellationToken>())
            .Returns((WordLookupResult?)null);

        var summary = await _sut.ImportAsync(ToCsvStream("unknownxyz"));

        Assert.Equal(0, summary.Imported);
        Assert.Equal(0, summary.Skipped);
        Assert.Equal(1, summary.Failed);
        Assert.Contains("unknownxyz", summary.FailedWords);
    }

    [Fact]
    public async Task ImportAsync_MWThrows_CountsAsFailed()
    {
        _mwClient.LookupAsync("badword", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API error"));

        var summary = await _sut.ImportAsync(ToCsvStream("badword"));

        Assert.Equal(1, summary.Failed);
        Assert.Contains("badword", summary.FailedWords);
    }

    [Fact]
    public async Task ImportAsync_MissingWordHeader_ThrowsInvalidOperation()
    {
        var csv = new MemoryStream(Encoding.UTF8.GetBytes("term\nephemeral\n"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ImportAsync(csv));
    }

    [Fact]
    public async Task ImportAsync_AudioDownloadFails_StillImportsWord()
    {
        _mwClient.LookupAsync("ephemeral", Arg.Any<CancellationToken>())
            .Returns(new WordLookupResult("adjective", "a definition", null, "epheme02"));
        _audioStore.DownloadAsync("epheme02", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("audio unavailable"));

        var summary = await _sut.ImportAsync(ToCsvStream("ephemeral"));

        Assert.Equal(1, summary.Imported);
        var saved = await _db.Words.SingleAsync(w => w.Text == "ephemeral");
        Assert.Null(saved.AudioFilePath);
    }

    public void Dispose() => _db.Dispose();

    private static Stream ToCsvStream(params string[] words)
    {
        var csv = "word\n" + string.Join("\n", words);
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
    }
}
