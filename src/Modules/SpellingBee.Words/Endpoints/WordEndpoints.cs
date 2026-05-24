using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SpellingBee.Words.Contracts;
using SpellingBee.Words.Services;

namespace SpellingBee.Words.Endpoints;

internal static class WordEndpoints
{
    internal static IEndpointRouteBuilder MapWordsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/words/import", async (
            IFormFile? file,
            IWordImportService importService,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("A non-empty CSV file is required.");

            await using var stream = file.OpenReadStream();
            try
            {
                var summary = await importService.ImportAsync(stream, ct);
                return Results.Ok(summary);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
        .WithName("ImportWords")
        .WithTags("Words")
        .DisableAntiforgery();

        app.MapPost("/api/words", async (
            AddWordRequest request,
            IWordService wordService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return Results.BadRequest("Text is required.");

            try
            {
                var word = await wordService.AddWordAsync(request.Text, ct);
                return Results.Created($"/api/words/{word.Id}", word);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Results.Conflict(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.UnprocessableEntity(ex.Message);
            }
        })
        .WithName("AddWord")
        .WithTags("Words");

        return app;
    }
}
