using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SpellingBee.Words;
using SpellingBee.Words.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        if (document.Paths.TryGetValue("/api/Words/import", out var pathItem)
            && pathItem.Operations.TryGetValue(HttpMethod.Post, out var post)
            && post.RequestBody is not null)
        {
            post.RequestBody.Content.Clear();
            post.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        ["file"] = new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Format = "binary"
                        }
                    }
                }
            };
        }
        return Task.CompletedTask;
    });
});
builder.Services.AddWordsModule(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<WordsDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();    
    app.MapScalarApiReference();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "SpellingBee API v1"));
}

//app.UseHttpsRedirection();
app.MapControllers();

app.Run();
