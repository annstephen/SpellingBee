using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpellingBee.Words.Controllers;
using SpellingBee.Words.Data;
using SpellingBee.Words.Infrastructure;
using SpellingBee.Words.Services;

namespace SpellingBee.Words;

public static class WordsModule
{
    public static IServiceCollection AddWordsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WordsDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("WordsDb")));

        services.Configure<MerriamWebsterOptions>(
            configuration.GetSection("MerriamWebster"));

        services.Configure<AudioStorageOptions>(
            configuration.GetSection("AudioStorage"));

        services.AddHttpClient<IMerriamWebsterClient, MerriamWebsterClient>();
        services.AddHttpClient<IAudioFileStore, AudioFileStore>();

        services.AddScoped<IWordImportService, WordImportService>();
        services.AddScoped<IWordService, WordService>();

        services.AddControllers()
                .AddApplicationPart(typeof(WordsController).Assembly);

        return services;
    }
}
