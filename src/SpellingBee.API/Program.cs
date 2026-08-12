using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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

        // Per-machine overlay for secrets (e.g. MerriamWebster:ApiKey) that can't be committed
        // to source control. Lives outside the install/publish directory so it survives
        // upgrades and reinstalls, and loads regardless of ASPNETCORE_ENVIRONMENT so it works
        // for the packaged desktop app (which always runs as Production) as well as `dotnet run`.
        builder.Configuration.AddJsonFile(
            Path.Combine(appDataRoot, "appsettings.Local.json"), optional: true, reloadOnChange: false);

        var dbPath = Path.Combine(appDataRoot, "spellingbee.db");
        builder.Configuration["ConnectionStrings:WordsDb"] = $"Data Source={dbPath}";
        builder.Configuration["ConnectionStrings:ProgressDb"] = $"Data Source={dbPath}";
        var audioRootPath = Path.Combine(appDataRoot, "audio");
        builder.Configuration["AudioStorage:RootPath"] = audioRootPath;
        Directory.CreateDirectory(audioRootPath);

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
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(audioRootPath),
            RequestPath = "/audio",
        });
        app.MapFallbackToFile("index.html");

        return app;
    }
}
