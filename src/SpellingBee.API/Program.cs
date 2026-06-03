using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SpellingBee.Words;
using SpellingBee.Words.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
