using Scalar.AspNetCore;
using SpellingBee.Words;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddWordsModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "SpellingBee API v1"));
}

app.UseHttpsRedirection();
app.MapWordsEndpoints();

app.Run();
