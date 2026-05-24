using Microsoft.EntityFrameworkCore;
using SpellingBee.Words.Domain;

namespace SpellingBee.Words.Data;

public sealed class WordsDbContext : DbContext
{
    public WordsDbContext(DbContextOptions<WordsDbContext> options) : base(options) { }

    public DbSet<Word> Words => Set<Word>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WordsDbContext).Assembly);
    }
}
