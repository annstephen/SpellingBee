using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpellingBee.Progress.Controllers;
using SpellingBee.Progress.Data;
using SpellingBee.Progress.Services;

namespace SpellingBee.Progress;

public static class ProgressModule
{
    public static IServiceCollection AddProgressModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProgressDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("ProgressDb")));

        services.AddScoped<IWordProgressService, WordProgressService>();

        services.AddControllers()
                .AddApplicationPart(typeof(ProgressController).Assembly);

        return services;
    }
}
