using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SpellingBee.Words.Contracts;
using SpellingBee.Words.Services;

namespace SpellingBee.Words.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WordsController : ControllerBase
{
    private readonly IWordImportService _importService;
    private readonly IWordService _wordService;

    public WordsController(IWordImportService importService, IWordService wordService)
    {
        _importService = importService;
        _wordService = wordService;
    }

    [HttpGet]
    [EndpointName("GetAllWords")]
    [Tags("Words")]
    [ProducesResponseType(typeof(IReadOnlyList<WordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var words = await _wordService.GetAllAsync(ct);
        return Ok(words);
    }

    [HttpPost("import")]
    [EndpointName("ImportWords")]
    [Tags("Words")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromForm] ImportWordsRequest? request, CancellationToken ct)
    {
        if (request?.File is not { Length: > 0 } file)
            return BadRequest("A non-empty CSV file is required.");

        await using var stream = file.OpenReadStream();
        try
        {
            var summary = await _importService.ImportAsync(stream, ct);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [EndpointName("AddWord")]
    [Tags("Words")]
    [ProducesResponseType(typeof(WordResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(AddWordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Text is required.");

        try
        {
            var word = await _wordService.AddWordAsync(request.Text, ct);
            return Created($"/api/words/{word.Id}", word);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }
}
