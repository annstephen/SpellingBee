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

    [HttpDelete("{id:int}")]
    [EndpointName("DeleteWord")]
    [Tags("Words")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _wordService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpDelete("batch")]
    [EndpointName("DeleteWords")]
    [Tags("Words")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBatch([FromBody] DeleteWordsRequest request, CancellationToken ct)
    {
        if (request.Ids is not { Count: > 0 })
            return BadRequest("At least one ID is required.");

        await _wordService.DeleteManyAsync(request.Ids, ct);
        return NoContent();
    }

    [HttpDelete("clear")]
    [EndpointName("ClearAllWords")]
    [Tags("Words")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await _wordService.ClearAllAsync(ct);
        return NoContent();
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
