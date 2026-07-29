using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SpellingBee.Progress.Contracts;
using SpellingBee.Progress.Services;

namespace SpellingBee.Progress.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgressController : ControllerBase
{
    private readonly IWordProgressService _progressService;

    public ProgressController(IWordProgressService progressService)
    {
        _progressService = progressService;
    }

    [HttpPost("attempts")]
    [EndpointName("RecordAttempt")]
    [Tags("Progress")]
    [ProducesResponseType(typeof(WordProgressResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordAttempt(RecordAttemptRequest request, CancellationToken ct)
    {
        var progress = await _progressService.RecordAttemptAsync(request.WordId, request.Correct, ct);
        return Ok(progress);
    }

    [HttpGet]
    [EndpointName("GetAllProgress")]
    [Tags("Progress")]
    [ProducesResponseType(typeof(IReadOnlyList<WordProgressResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var progress = await _progressService.GetAllAsync(ct);
        return Ok(progress);
    }

    [HttpGet("{wordId:int}")]
    [EndpointName("GetProgressForWord")]
    [Tags("Progress")]
    [ProducesResponseType(typeof(WordProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByWordId(int wordId, CancellationToken ct)
    {
        var progress = await _progressService.GetByWordIdAsync(wordId, ct);
        return progress is null ? NotFound() : Ok(progress);
    }
}
