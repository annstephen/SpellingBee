using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SpellingBee.Progress;
using SpellingBee.Progress.Data;
using SpellingBee.Words;
using SpellingBee.Words.Data;

await ApiHost.Build(args).RunAsync();

public static class ApiHost
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        });

        var appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpellingBee");
        Directory.CreateDirectory(appDataRoot);
        var dbPath = Path.Combine(appDataRoot, "spellingbee.db");
        builder.Configuration["ConnectionStrings:WordsDb"] = $"Data Source={dbPath}";
        builder.Configuration["ConnectionStrings:ProgressDb"] = $"Data Source={dbPath}";
        builder.Configuration["AudioStorage:RootPath"] = Path.Combine(appDataRoot, "audio");

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddWordsModule(builder.Configuration);
        builder.Services.AddProgressModule(builder.Configuration);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<WordsDbContext>().Database.Migrate();
            scope.ServiceProvider.GetRequiredService<ProgressDbContext>().Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "SpellingBee API v1"));
            app.UseCors();
        }

        //app.UseHttpsRedirection();
        app.MapControllers();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        return app;
    }
}
