using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpellingBee.Progress.Domain;

namespace SpellingBee.Progress.Data.Configurations;

internal sealed class WordProgressConfiguration : IEntityTypeConfiguration<WordProgress>
{
    public void Configure(EntityTypeBuilder<WordProgress> builder)
    {
        builder.ToTable("WordProgress");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.WordId).IsRequired();
        builder.HasIndex(p => p.WordId).IsUnique();
        builder.Property(p => p.AttemptCount).IsRequired();
        builder.Property(p => p.CorrectCount).IsRequired();
        builder.Property(p => p.IncorrectCount).IsRequired();
    }
}
