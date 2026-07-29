using Microsoft.EntityFrameworkCore;
using SpellingBee.Progress.Domain;

namespace SpellingBee.Progress.Data;

public sealed class ProgressDbContext : DbContext
{
    public ProgressDbContext(DbContextOptions<ProgressDbContext> options) : base(options) { }

    public DbSet<WordProgress> WordProgress => Set<WordProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProgressDbContext).Assembly);
    }
}
