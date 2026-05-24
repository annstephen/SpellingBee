using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        return app;
    }
}
